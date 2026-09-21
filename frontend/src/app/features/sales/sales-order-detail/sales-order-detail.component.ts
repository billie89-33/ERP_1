import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-sales-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div *ngIf="isLoading" class="p-8 text-center text-slate-500">Loading Order Details...</div>

    <div *ngIf="!isLoading && order" class="max-w-5xl mx-auto">
      <div class="mb-6 flex justify-between items-center">
        <div>
          <h1 class="text-2xl font-bold text-slate-800">Sales Order: {{ order.orderNumber }}</h1>
          <p class="text-slate-500 text-sm mt-1">Status: 
            <span class="px-2 py-1 rounded-full text-xs font-semibold"
                  [ngClass]="{
                    'bg-yellow-100 text-yellow-800': order.status === 'Pending',
                    'bg-green-100 text-green-800': order.status === 'Shipped',
                    'bg-red-100 text-red-800': order.status === 'Cancelled'
                  }">
              {{ order.status }}
            </span>
          </p>
        </div>
        <div class="space-x-3">
          <a routerLink="/admin/sales-orders" class="px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-700 rounded-md shadow-sm font-medium">Back to List</a>
          <button *ngIf="order.status === 'Pending'" (click)="shipOrder()" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md shadow-sm font-medium">📦 Ship Items (Goods Issue)</button>
          <button *ngIf="order.status === 'Pending'" (click)="cancelOrder()" class="px-4 py-2 bg-red-100 hover:bg-red-200 text-red-700 rounded-md shadow-sm font-medium border border-red-200">❌ Cancel Order</button>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow overflow-hidden mb-6">
        <div class="p-6 border-b border-slate-200 flex justify-between">
          <div>
            <h3 class="text-sm font-medium text-slate-500 uppercase">Customer</h3>
            <p class="text-lg font-semibold text-slate-800 mt-1">{{ order.customerName }}</p>
          </div>
        </div>

        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 text-slate-500 text-xs uppercase tracking-wider">
              <th class="p-4 font-medium border-b border-slate-200">Product</th>
              <th class="p-4 font-medium border-b border-slate-200">Qty</th>
              <th class="p-4 font-medium border-b border-slate-200">Unit Price</th>
              <th class="p-4 font-medium border-b border-slate-200 text-right">Total</th>
            </tr>
          </thead>
          <tbody class="text-sm divide-y divide-slate-100">
            <tr *ngFor="let item of order.items" class="hover:bg-slate-50">
              <td class="p-4 text-slate-800">{{ item.productName }}</td>
              <td class="p-4 text-slate-600">{{ item.quantity }}</td>
              <td class="p-4 text-slate-600">฿{{ item.unitPrice | number:'1.2-2' }}</td>
              <td class="p-4 text-slate-800 font-medium text-right">฿{{ (item.quantity * item.unitPrice) | number:'1.2-2' }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="bg-slate-50">
              <td colspan="3" class="p-4 text-right font-bold text-slate-700">Grand Total</td>
              <td class="p-4 text-right font-bold text-blue-600 text-lg">฿{{ getTotal() | number:'1.2-2' }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  `
})
export class SalesOrderDetailComponent implements OnInit {
  order: any = null;
  isLoading = true;

  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private router = inject(Router);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOrder(id);
    }
  }

  loadOrder(id: string) {
    this.http.get(`http://localhost:5243/api/SalesOrders/${id}`).subscribe({
      next: (data) => {
        this.order = data;
        this.isLoading = false;
      },
      error: (err) => {
        alert('Failed to load order details');
        this.isLoading = false;
        this.router.navigate(['/admin/sales-orders']);
      }
    });
  }

  getTotal(): number {
    if (!this.order || !this.order.items) return 0;
    return this.order.items.reduce((sum: number, item: any) => sum + (item.quantity * item.unitPrice), 0);
  }

  cancelOrder() {
    if (confirm('Are you sure you want to cancel this order? This will return the reserved stock to available inventory.')) {
      this.http.put(`http://localhost:5243/api/SalesOrders/${this.order.id}/cancel`, {}).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadOrder(this.order.id);
        },
        error: (err) => alert(err.error?.message || 'Failed to cancel order')
      });
    }
  }

  shipOrder() {
    if (confirm('Are you sure you want to Ship this order? This will deduct the physical stock from the warehouse.')) {
      const giPayload = {
        salesOrderId: this.order.id,
        remarks: 'Shipped from SO Details',
        items: this.order.items.map((i: any) => ({
          productId: i.productId,
          quantity: i.quantity
        }))
      };

      this.http.post('http://localhost:5243/api/GoodsIssues', giPayload).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadOrder(this.order.id);
        },
        error: (err) => alert(err.error?.message || 'Failed to ship order')
      });
    }
  }
}
