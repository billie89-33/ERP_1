import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-purchase-order-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './purchase-order-form.component.html',
  styleUrl: './purchase-order-form.component.scss'
})
export class PurchaseOrderFormComponent implements OnInit {
  poForm: FormGroup;
  suppliers: any[] = [];
  products: any[] = [];
  isLoading = false;

  supplierSearchCtrl = new FormControl('');
  showSupplierDropdown = false;
  selectedSupplierName = '';

  isSupplierModalOpen = false;
  isSavingSupplier = false;
  supplierError = '';
  supplierForm: FormGroup;

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  constructor() {
    this.poForm = this.fb.group({
      supplierId: ['', Validators.required],
      items: this.fb.array([])
    });

    // Handle supplier search filtering natively
    this.supplierSearchCtrl.valueChanges.subscribe((val: string | null) => {
      if (!val) {
        this.poForm.patchValue({ supplierId: '' });
      }
    });

    this.supplierForm = this.fb.group({
      companyName: ['', Validators.required],
      taxId: ['', Validators.required],
      contactName: [''],
      phone: [''],
      email: [''],
      address: ['']
    });
  }

  ngOnInit() {
    this.loadSuppliers();
    this.loadProducts();
    this.addItem(); // Add at least one empty item
  }

  get items(): FormArray {
    return this.poForm.get('items') as FormArray;
  }

  addItem() {
    this.items.push(this.fb.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitCost: [0, [Validators.required, Validators.min(0)]]
    }));
  }

  removeItem(index: number) {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  onProductSelected(index: number) {
    const productId = this.items.at(index).get('productId')?.value;
    const product = this.products.find(p => p.id === productId);
    if (product) {
      this.items.at(index).patchValue({
        unitCost: product.cost // PO uses cost instead of price
      });
    }
  }

  getTotal(): number {
    return this.items.controls.reduce((sum, item) => {
      const qty = item.get('quantity')?.value || 0;
      const cost = item.get('unitCost')?.value || 0;
      return sum + (qty * cost);
    }, 0);
  }

  loadSuppliers() {
    this.http.get<any[]>('/api/Suppliers').subscribe({
      next: (res: any) => this.suppliers = res.data || res,
      error: () => console.error('Failed to load suppliers')
    });
  }

  get filteredSuppliers() {
    const term = this.supplierSearchCtrl.value?.toLowerCase() || '';
    if (!term) return this.suppliers;
    return this.suppliers.filter(s => 
      s.companyName.toLowerCase().includes(term) || 
      s.taxId.includes(term)
    );
  }

  selectSupplier(supplier: any) {
    this.supplierSearchCtrl.setValue(supplier.companyName, { emitEvent: false });
    this.poForm.patchValue({ supplierId: supplier.id });
    this.showSupplierDropdown = false;
  }

  hideSupplierDropdown() {
    // Timeout to allow mousedown event on list items to fire first
    setTimeout(() => {
      this.showSupplierDropdown = false;
      // If user typed something but didn't select, reset if ID is missing
      if (!this.poForm.value.supplierId) {
        this.supplierSearchCtrl.setValue('');
      } else {
        // Revert text to the selected supplier's name
        const s = this.suppliers.find(x => x.id === this.poForm.value.supplierId);
        if (s) this.supplierSearchCtrl.setValue(s.companyName, { emitEvent: false });
      }
    }, 200);
  }

  loadProducts() {
    this.http.get<any>('/api/Products?limit=100').subscribe({
      next: (res) => {
        this.products = res.data || res;
      },
      error: () => console.error('Failed to load products')
    });
  }

  // --- Supplier Modal Logic ---
  openSupplierModal() {
    this.supplierError = '';
    this.supplierForm.reset();
    this.isSupplierModalOpen = true;
  }

  closeSupplierModal() {
    this.isSupplierModalOpen = false;
  }

  submitSupplier() {
    if (this.supplierForm.invalid) return;

    this.isSavingSupplier = true;
    this.supplierError = '';

    this.http.post('/api/Suppliers', this.supplierForm.value).subscribe({
      next: (res: any) => {
        this.isSavingSupplier = false;
        const newSupplier = res.data || res;
        
        // Add to local list and select it
        this.suppliers = [newSupplier, ...this.suppliers];
        this.poForm.patchValue({ supplierId: newSupplier.id });
        this.supplierSearchCtrl.setValue(newSupplier.companyName, { emitEvent: false });
        
        this.closeSupplierModal();
      },
      error: (err) => {
        this.isSavingSupplier = false;
        // The API returns { message: "..." } on 400 Bad Request
        this.supplierError = err.error?.message || 'Failed to create supplier. It might already exist.';
      }
    });
  }

  onSubmit() {
    if (this.poForm.invalid) {
      this.poForm.markAllAsTouched();
      alert('Please fill in all required fields correctly.');
      return;
    }
    
    this.isLoading = true;
    this.http.post('/api/PurchaseOrders', this.poForm.value).subscribe({
      next: (res: any) => {
        alert(res.message || 'Purchase Order created successfully!');
        this.router.navigate(['/admin/purchase-orders']);
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create Purchase Order');
        this.isLoading = false;
      }
    });
  }
}

