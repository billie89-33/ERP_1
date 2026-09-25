import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-sales-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div *ngIf="isLoading" class="p-8 text-center text-slate-500">Loading Order Details...</div>

    <div *ngIf="!isLoading && order" class="max-w-5xl mx-auto">
      
      <!-- Print Header (Visible only when printing) -->
      <div class="hidden print:block mb-8 text-center" *ngIf="companySettings">
        <img *ngIf="companySettings.logoUrl" [src]="companySettings.logoUrl" class="h-16 mx-auto mb-2" alt="Company Logo">
        <h1 class="text-2xl font-bold">{{ companySettings.companyName }}</h1>
        <p class="text-sm text-gray-600">{{ companySettings.address }}</p>
        <p class="text-sm text-gray-600">Tax ID: {{ companySettings.taxId }} | Phone: {{ companySettings.phone }} | Email: {{ companySettings.email }}</p>
        <div class="border-b-2 border-gray-800 my-4"></div>
        <h2 class="text-xl font-bold uppercase tracking-wider">Receipt / Delivery Note</h2>
      </div>

      <div class="mb-6 flex justify-between items-center no-print">
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
        <div class="space-x-3 no-print">
          <button (click)="printDocument()" class="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-md shadow-sm font-medium"><i class="fas fa-print mr-2"></i> Print PDF</button>
          <a routerLink="/admin/sales-orders" class="px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-700 rounded-md shadow-sm font-medium">Back to List</a>
          
          <!-- Only Admin or Warehouse can ship -->
          <button *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" (click)="shipOrder()" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md shadow-sm font-medium">📦 Ship Items (Goods Issue)</button>
          
          <!-- Only Admin or Sales can cancel -->
          <button *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Sales'])" (click)="cancelOrder()" class="px-4 py-2 bg-red-100 hover:bg-red-200 text-red-700 rounded-md shadow-sm font-medium border border-red-200">❌ Cancel Order</button>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow overflow-hidden mb-6">
        <div class="p-6 border-b border-slate-200 flex justify-between items-start">
          <div>
            <h3 class="text-sm font-medium text-slate-500 uppercase">Customer</h3>
            <p class="text-lg font-semibold text-slate-800 mt-1">{{ order.customerName }}</p>
          </div>
          
          <div class="text-right">
            <h3 class="text-sm font-medium text-slate-500 uppercase">Payment Status</h3>
            <p class="text-lg font-bold mt-1"
               [ngClass]="{
                 'text-red-500': order.paymentStatus === 'Pending',
                 'text-yellow-600': order.paymentStatus === 'Checking',
                 'text-green-600': order.paymentStatus === 'Paid'
               }">
              {{ order.paymentStatus || 'Pending' }}
            </p>
          </div>
        </div>
        
        <div *ngIf="order.paymentStatus === 'Checking' && order.paymentSlipUrl" class="p-6 bg-yellow-50 border-b border-slate-200 flex flex-col md:flex-row gap-6 items-center no-print">
          <div>
            <h3 class="text-sm font-bold text-yellow-800 mb-2">Customer uploaded a payment slip:</h3>
            <img [src]="order.paymentSlipUrl" class="w-64 h-auto rounded-lg border border-yellow-200 shadow-sm" alt="Payment Slip">
          </div>
          <div>
            <button *ngIf="hasRole(['Admin', 'Sales'])" (click)="verifyPayment()" class="px-6 py-3 bg-green-600 hover:bg-green-700 text-white font-bold rounded-lg shadow-md">
              ✅ Verify Payment (Mark as Paid)
            </button>
            <p class="text-xs text-yellow-600 mt-2">Make sure the money is in the bank account before verifying.</p>
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
  companySettings: any = null;
  isLoading = true;

  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private router = inject(Router);
  private authService = inject(AuthService);

  ngOnInit() {
    this.loadCompanySettings();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOrder(id);
    }
  }

  loadCompanySettings() {
    this.http.get('/api/Settings/company').subscribe({
      next: (data) => this.companySettings = data,
      error: (err) => console.error('Failed to load company settings', err)
    });
  }

  hasRole(allowedRoles: string[]): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;
    return allowedRoles.includes(user.role);
  }

  loadOrder(id: string) {
    this.http.get(`/api/SalesOrders/${id}`).subscribe({
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
      this.http.put(`/api/SalesOrders/${this.order.id}/cancel`, {}).subscribe({
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

      this.http.post('/api/GoodsIssues', giPayload).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadOrder(this.order.id);
        },
        error: (err) => alert(err.error?.message || 'Failed to ship order')
      });
    }
  }

  verifyPayment() {
    if (confirm('Are you sure this payment is valid and money is in the bank?')) {
      this.http.post(`/api/SalesOrders/${this.order.id}/verify-payment`, {}, { withCredentials: true }).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadOrder(this.order.id);
        },
        error: (err) => alert(err.error?.message || 'Failed to verify payment')
      });
    }
  }

  printDocument() {
    window.print();
  }
}
