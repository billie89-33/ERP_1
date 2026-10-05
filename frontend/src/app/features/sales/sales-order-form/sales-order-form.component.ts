import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-sales-order-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './sales-order-form.component.html',
  styleUrl: './sales-order-form.component.scss'
})
export class SalesOrderFormComponent implements OnInit {
  soForm: FormGroup;
  quickCustomerForm: FormGroup;
  showQuickCustomerModal = false;
  isSavingCustomer = false;
  customers: any[] = [];
  products: any[] = [];
  isLoading = false;

  customerSearchCtrl = new FormControl('');
  showCustomerDropdown = false;

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  constructor() {
    this.soForm = this.fb.group({
      customerId: ['', Validators.required],
      items: this.fb.array([])
    });
      
      this.quickCustomerForm = this.fb.group({
        customerType: ['B2B', Validators.required],
        companyName: ['', Validators.required],
        taxId: [''],
        phone: [''],
        address: ['']
      });

      this.quickCustomerForm.get('customerType')?.valueChanges.subscribe(type => {
        const taxIdControl = this.quickCustomerForm.get('taxId');
        if (type === 'B2B') {
          taxIdControl?.setValidators([Validators.required, Validators.minLength(13), Validators.maxLength(13)]);
        } else {
          taxIdControl?.clearValidators();
        }
        taxIdControl?.updateValueAndValidity();
      });


    // Handle customer search filtering natively
    this.customerSearchCtrl.valueChanges.subscribe((val: string | null) => {
      if (!val) {
        this.soForm.patchValue({ customerId: '' });
      }
    });
  }

  ngOnInit() {
    this.loadCustomers();
    this.loadProducts();
    this.addItem(); // Add at least one empty item
  }

  get items(): FormArray {
    return this.soForm.get('items') as FormArray;
  }

  addItem() {
    this.items.push(this.fb.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]]
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
        unitPrice: product.price
      });
    }
  }

  getTotal(): number {
    return this.items.controls.reduce((sum, item) => {
      const qty = item.get('quantity')?.value || 0;
      const price = item.get('unitPrice')?.value || 0;
      return sum + (qty * price);
    }, 0);
  }

  
  openQuickCustomerModal() {
    this.quickCustomerForm.reset({ customerType: 'B2B' });
    this.showQuickCustomerModal = true;
  }

  closeQuickCustomerModal() {
    this.showQuickCustomerModal = false;
  }

  saveQuickCustomer() {
    if (this.quickCustomerForm.invalid) {
      alert('กรุณากรอกข้อมูลที่จำเป็นให้ครบถ้วน');
      return;
    }

    this.isSavingCustomer = true;
    const data = this.quickCustomerForm.value;

    this.http.post('/api/Customers', data).subscribe({
      next: (res: any) => {
        alert('เพิ่มลูกค้าใหม่เรียบร้อยแล้ว');
        this.isSavingCustomer = false;
        this.showQuickCustomerModal = false;
        
        // Reload customers and auto-select
        this.http.get<any[]>('/api/Customers').subscribe({
          next: (customersRes: any) => {
            this.customers = customersRes.data || customersRes;
            const newCustomer = this.customers.find(c => c.id === res.id);
            if (newCustomer) {
              this.selectCustomer(newCustomer);
            }
          }
        });
      },
      error: (err) => {
        this.isSavingCustomer = false;
        alert(err.error?.message || 'Failed to save customer');
      }
    });
  }

  loadCustomers() {
    this.http.get<any[]>('/api/Customers').subscribe({
      next: (res: any) => this.customers = res.data || res,
      error: () => console.error('Failed to load customers')
    });
  }

  get filteredCustomers() {
    const term = this.customerSearchCtrl.value?.toLowerCase() || '';
    if (!term) return this.customers;
    return this.customers.filter(c => 
      (c.companyName || '').toLowerCase().includes(term) || 
      (c.taxId || '').includes(term) ||
      (c.firstName || '').toLowerCase().includes(term)
    );
  }

  selectCustomer(customer: any) {
    const displayName = customer.customerType === 'B2B' ? customer.companyName : (customer.companyName || customer.firstName + ' ' + customer.lastName);
    this.customerSearchCtrl.setValue(displayName, { emitEvent: false });
    this.soForm.patchValue({ customerId: customer.id });
    this.showCustomerDropdown = false;
  }

  hideCustomerDropdown() {
    setTimeout(() => {
      this.showCustomerDropdown = false;
      if (!this.soForm.value.customerId) {
        this.customerSearchCtrl.setValue('');
      } else {
        const c = this.customers.find(x => x.id === this.soForm.value.customerId);
        if (c) {
          const displayName = c.customerType === 'B2B' ? c.companyName : (c.companyName || c.firstName + ' ' + c.lastName);
          this.customerSearchCtrl.setValue(displayName, { emitEvent: false });
        }
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

  onSubmit() {
    if (this.soForm.invalid) {
      this.soForm.markAllAsTouched();
      alert('Please fill in all required fields correctly.');
      return;
    }
    
    this.isLoading = true;
    this.http.post('/api/SalesOrders', this.soForm.value).subscribe({
      next: (res: any) => {
        alert(res.message || 'Sales Order created successfully!');
        this.router.navigate(['/admin/sales-orders']);
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create Sales Order');
        this.isLoading = false;
      }
    });
  }
}

