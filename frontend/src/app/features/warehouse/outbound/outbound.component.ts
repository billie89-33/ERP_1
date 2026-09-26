import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-warehouse-outbound',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-slate-800">Outbound (รอแพ็คจัดส่ง)</h1>
      <p class="text-slate-500">Sales Orders that have been paid and are ready to be picked, packed, and shipped.</p>
    </div>

    <div *ngIf="isLoading" class="p-8 text-center text-slate-500 font-bold">
      Loading...
    </div>
    
    <div *ngIf="!isLoading" class="bg-white rounded-lg shadow overflow-hidden border border-slate-200">
      <div class="px-6 py-4 border-b border-slate-200 bg-orange-50">
        <h2 class="text-lg font-bold text-orange-800 flex items-center">
          <i class="fas fa-box-open mr-2"></i> Action Required: Pack & Ship
        </h2>
      </div>

      <table class="min-w-full divide-y divide-slate-200">
        <thead class="bg-slate-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">SO Number</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Date</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Customer</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Payment</th>
            <th class="px-6 py-3 text-right text-xs font-bold text-slate-500 uppercase tracking-wider">Action</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-slate-200">
          <tr *ngFor="let so of pendingSOs" class="hover:bg-slate-50 transition">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-bold text-orange-600">{{ so.orderNumber }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-slate-500">{{ so.orderDate | date:'shortDate' }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-slate-900 font-medium">{{ so.customerName }}</td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span class="bg-green-100 text-green-800 px-2 py-1 rounded text-xs font-bold border border-green-200">
                Paid (พร้อมส่ง)
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm">
              <button (click)="shipItems(so.id)" class="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded font-bold shadow-sm transition">
                แพ็ค & จัดส่ง (Ship)
              </button>
            </td>
          </tr>
          <tr *ngIf="pendingSOs.length === 0">
            <td colspan="5" class="px-6 py-8 text-center text-slate-500 font-medium">No pending shipments found.</td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class WarehouseOutboundComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);
  
  pendingSOs: any[] = [];
  isLoading = true;

  ngOnInit() {
    this.loadPendingSOs();
  }

  loadPendingSOs() {
    this.http.get<any>('/api/SalesOrders?page=1&pageSize=100').subscribe({
      next: (data) => {
        // Filter only Paid and Pending SOs
        this.pendingSOs = data.items.filter((so: any) => so.status === 'Pending' && so.paymentStatus === 'Paid');
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load SOs', err);
        this.isLoading = false;
      }
    });
  }

  shipItems(soId: string) {
    if (confirm('คุณแพ็คสินค้าและจัดส่งเรียบร้อยแล้ว ต้องการตัดสต๊อกใช่หรือไม่?')) {
      // Fetch SO items first
      this.http.get<any>(`/api/SalesOrders/${soId}`).subscribe({
        next: (so) => {
          const giPayload = {
            salesOrderId: soId,
            remarks: 'Auto-shipped from Warehouse Outbound',
            items: so.items.map((i: any) => ({
              productId: i.productId,
              quantity: i.quantity
            }))
          };
          
          this.http.post('/api/GoodsIssues', giPayload).subscribe({
            next: () => {
              alert('ตัดสต๊อกและเปลี่ยนสถานะเป็น Shipped เรียบร้อยแล้ว!');
              this.loadPendingSOs(); // reload
            },
            error: (err) => alert('Error: ' + err.error?.message)
          });
        }
      });
    }
  }
}
