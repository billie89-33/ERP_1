import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
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

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  constructor() {
    this.poForm = this.fb.group({
      supplierId: ['', Validators.required],
      items: this.fb.array([])
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
    this.http.get<any[]>('http://localhost:5243/api/Suppliers').subscribe({
      next: (data) => this.suppliers = data,
      error: () => this.suppliers = [{ id: 'mock', name: 'Mock Supplier (Please add API)' }]
    });
  }

  loadProducts() {
    this.http.get<any[]>('http://localhost:5243/api/Products').subscribe({
      next: (data) => this.products = data,
      error: () => this.products = [{ id: 'mock', name: 'Mock Product', cost: 80 }]
    });
  }

  onSubmit() {
    if (this.poForm.invalid) {
      alert('Please fill in all required fields.');
      return;
    }
    
    this.isLoading = true;
    this.http.post('http://localhost:5243/api/PurchaseOrders', this.poForm.value).subscribe({
      next: (res: any) => {
        alert(res.message);
        this.router.navigate(['/admin/purchase-orders']);
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create Purchase Order');
        this.isLoading = false;
      }
    });
  }
}

