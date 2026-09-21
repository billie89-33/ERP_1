import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
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
  customers: any[] = [];
  products: any[] = [];
  isLoading = false;

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  constructor() {
    this.soForm = this.fb.group({
      customerId: ['', Validators.required],
      items: this.fb.array([])
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

  loadCustomers() {
    this.http.get<any[]>('http://localhost:5243/api/Customers').subscribe({
      next: (data) => this.customers = data,
      error: () => this.customers = [{ id: 'mock', companyName: 'Mock Customer (Please add API)' }]
    });
  }

  loadProducts() {
    this.http.get<any[]>('http://localhost:5243/api/Products').subscribe({
      next: (data) => this.products = data,
      error: () => this.products = [{ id: 'mock', name: 'Mock Product', price: 100 }]
    });
  }

  onSubmit() {
    if (this.soForm.invalid) {
      alert('Please fill in all required fields.');
      return;
    }
    
    this.isLoading = true;
    this.http.post('http://localhost:5243/api/SalesOrders', this.soForm.value).subscribe({
      next: (res: any) => {
        alert(res.message);
        this.router.navigate(['/admin/sales-orders']);
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create Sales Order');
        this.isLoading = false;
      }
    });
  }
}

