import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-goods-receipt',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-4xl mx-auto p-6 bg-white rounded-lg shadow">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-bold text-slate-800">Receive Goods (GR)</h1>
        <a routerLink="/admin/purchase-orders" class="text-slate-500 hover:text-slate-700">Back to PO List</a>
      </div>

      <div *ngIf="isLoading" class="text-center py-8">Loading PO details...</div>

      <div *ngIf="!isLoading && po" class="space-y-6">
        <div class="bg-slate-50 p-4 rounded-md border border-slate-200">
          <h2 class="font-semibold text-lg mb-2">PO Information</h2>
          <div class="grid grid-cols-2 gap-4">
            <div><span class="text-slate-500">PO Number:</span> <span class="font-medium">{{ po.poNumber }}</span></div>
            <div><span class="text-slate-500">Supplier:</span> <span class="font-medium">{{ po.supplierName }}</span></div>
            <div><span class="text-slate-500">Status:</span> <span class="font-medium">{{ po.status }}</span></div>
          </div>
        </div>

        <div>
          <h2 class="font-semibold text-lg mb-4">Items to Receive</h2>
          <table class="min-w-full divide-y divide-slate-200 border border-slate-200 rounded-lg overflow-hidden">
            <thead class="bg-slate-100">
              <tr>
                <th class="px-4 py-2 text-left text-xs font-medium text-slate-500 uppercase">Product</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-slate-500 uppercase">Ordered Qty</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-slate-500 uppercase">Receive Qty</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-200">
              <tr *ngFor="let item of itemsToReceive">
                <td class="px-4 py-3 text-sm text-slate-900">{{ item.productName }}</td>
                <td class="px-4 py-3 text-sm text-right text-slate-500">{{ item.orderedQuantity }}</td>
                <td class="px-4 py-3 text-sm text-right">
                  <input type="number" [(ngModel)]="item.receiveQuantity" min="0" [max]="item.orderedQuantity"
                         class="w-24 text-right border-slate-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm">
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700">Remarks (Optional)</label>
          <textarea [(ngModel)]="remarks" rows="2" class="mt-1 block w-full border-slate-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm"></textarea>
        </div>

        <div class="flex justify-end pt-4 border-t border-slate-200">
          <button (click)="submitReceipt()" [disabled]="isSubmitting"
                  class="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-md shadow-sm disabled:opacity-50">
            {{ isSubmitting ? 'Processing...' : 'Confirm Receipt' }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class GoodsReceiptComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  poId: string = '';
  po: any = null;
  itemsToReceive: any[] = [];
  remarks: string = '';
  
  isLoading = true;
  isSubmitting = false;

  ngOnInit() {
    this.poId = this.route.snapshot.paramMap.get('id') || '';
    if (this.poId) {
      this.loadPO();
    }
  }

  loadPO() {
    this.http.get<any>(`http://localhost:5243/api/PurchaseOrders/${this.poId}`).subscribe({
      next: (data) => {
        this.po = data;
        // Map items
        this.itemsToReceive = data.items.map((i: any) => ({
          productId: i.productId,
          productName: i.productName,
          orderedQuantity: i.quantity,
          receiveQuantity: i.quantity // default to full receipt
        }));
        this.isLoading = false;
      },
      error: (err) => {
        alert('Failed to load PO details');
        this.router.navigate(['/admin/purchase-orders']);
      }
    });
  }

  submitReceipt() {
    // Check quantities
    const items = this.itemsToReceive
      .filter(i => i.receiveQuantity > 0)
      .map(i => ({
        productId: i.productId,
        quantity: i.receiveQuantity
      }));

    if (items.length === 0) {
      alert('Please enter at least 1 item to receive.');
      return;
    }

    this.isSubmitting = true;
    const payload = {
      purchaseOrderId: this.poId,
      remarks: this.remarks,
      items: items
    };

    this.http.post('http://localhost:5243/api/GoodsReceipts', payload).subscribe({
      next: (res: any) => {
        alert('Goods Receipt created successfully! Stock has been updated.');
        this.router.navigate(['/admin/purchase-orders']);
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create Goods Receipt');
        this.isSubmitting = false;
      }
    });
  }
}
