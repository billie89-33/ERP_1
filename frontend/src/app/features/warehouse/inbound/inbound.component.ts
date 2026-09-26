import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-warehouse-inbound',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-slate-800">Inbound (รอรับเข้า)</h1>
      <p class="text-slate-500">Purchase Orders waiting to be received into the warehouse.</p>
    </div>

    <div *ngIf="isLoading" class="p-8 text-center text-slate-500 font-bold">
      Loading...
    </div>
    
    <div *ngIf="!isLoading" class="bg-white rounded-lg shadow overflow-hidden border border-slate-200">
      <div class="px-6 py-4 border-b border-slate-200 bg-blue-50">
        <h2 class="text-lg font-bold text-blue-800 flex items-center">
          <i class="fas fa-truck-loading mr-2"></i> Pending Deliveries
        </h2>
      </div>

      <table class="min-w-full divide-y divide-slate-200">
        <thead class="bg-slate-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">PO Number</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Date</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Supplier</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Status</th>
            <th class="px-6 py-3 text-right text-xs font-bold text-slate-500 uppercase tracking-wider">Action</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-slate-200">
          <tr *ngFor="let po of pendingPOs" class="hover:bg-slate-50 transition">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-bold text-blue-600">{{ po.poNumber }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-slate-500">{{ po.orderDate | date:'shortDate' }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-slate-900 font-medium">{{ po.supplierName }}</td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span class="bg-yellow-100 text-yellow-800 px-2 py-1 rounded text-xs font-bold border border-yellow-200">
                Awaiting Receipt
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm">
              <button (click)="receiveItems(po.id)" class="bg-emerald-600 hover:bg-emerald-700 text-white px-4 py-2 rounded font-bold shadow-sm transition">
                รับเข้าโกดัง (Receive)
              </button>
            </td>
          </tr>
          <tr *ngIf="pendingPOs.length === 0">
            <td colspan="5" class="px-6 py-8 text-center text-slate-500 font-medium">No pending deliveries found.</td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class WarehouseInboundComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);
  
  pendingPOs: any[] = [];
  isLoading = true;

  ngOnInit() {
    this.loadPendingPOs();
  }

  loadPendingPOs() {
    this.http.get<any[]>('/api/PurchaseOrders').subscribe({
      next: (data) => {
        // Filter only Approved POs (ready to receive)
        this.pendingPOs = data.filter(po => po.status === 'Approved');
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load POs', err);
        this.isLoading = false;
      }
    });
  }

  receiveItems(poId: string) {
    if (confirm('คุณได้รับสินค้าเรียบร้อยแล้วและต้องการนำเข้าสต๊อกใช่หรือไม่?')) {
      // For simplicity, we create a full Goods Receipt with 1 click
      // We need to fetch the PO items first to know the quantities
      this.http.get<any>(`/api/PurchaseOrders/${poId}`).subscribe({
        next: (po) => {
          const grPayload = {
            purchaseOrderId: poId,
            remarks: 'Auto-received from Warehouse Inbound',
            items: po.items.map((i: any) => ({
              productId: i.productId,
              quantity: i.quantity
            }))
          };
          
          this.http.post('/api/GoodsReceipts', grPayload).subscribe({
            next: () => {
              alert('รับสินค้าเข้าสต๊อกเรียบร้อยแล้ว!');
              this.loadPendingPOs();
            },
            error: (err) => alert('Error: ' + err.error?.message)
          });
        }
      });
    }
  }
}
