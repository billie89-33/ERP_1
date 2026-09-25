import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-purchase-order-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
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
        <h2 class="text-xl font-bold uppercase tracking-wider">Purchase Order</h2>
      </div>

      <div class="mb-4 flex justify-between items-center no-print">
        <a routerLink="/admin/purchase-orders" class="text-blue-600 hover:text-blue-800 text-sm font-medium"><i class="fas fa-arrow-left mr-1"></i> กลับหน้ารายการใบสั่งซื้อ</a>
        <div class="space-x-3">
          <button (click)="printDocument()" class="px-3 py-1 bg-slate-800 hover:bg-slate-700 text-white rounded text-sm font-medium shadow-sm transition"><i class="fas fa-print mr-1"></i> Print PDF</button>
          
          <!-- Only Admin or Purchasing can cancel -->
          <button *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Purchasing'])" (click)="cancelOrder()" class="px-3 py-1 bg-white hover:bg-red-50 text-red-600 rounded text-sm font-medium border border-red-200 transition">❌ ยกเลิกใบสั่งซื้อนี้</button>
        </div>
      </div>

      <div class="bg-white shadow-md rounded-lg overflow-hidden border border-slate-200">
        
        <!-- Header (Dynamic color based on status) -->
        <div [ngClass]="order.status === 'Pending' ? 'bg-emerald-700' : 'bg-slate-800'" class="text-white px-6 py-4 flex justify-between items-center">
          <h2 class="text-lg font-bold">
            <i *ngIf="order.status === 'Pending'" class="fas fa-truck-loading mr-2"></i>
            <i *ngIf="order.status !== 'Pending'" class="fas fa-file-invoice mr-2"></i>
            {{ order.status === 'Pending' ? 'บันทึกรับสินค้าเข้าคลัง (ฝ่ายคลังสินค้า)' : 'รายละเอียดใบสั่งซื้อ (Purchase Order)' }}
          </h2>
          
          <!-- Only Admin or Warehouse can receive -->
          <button *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" (click)="receiveOrder()" [disabled]="isSubmitting" class="bg-emerald-500 hover:bg-emerald-400 px-4 py-1.5 rounded text-sm font-bold transition shadow disabled:bg-emerald-800">
            <i class="fas fa-check-circle mr-2"></i> {{ isSubmitting ? 'กำลังบันทึก...' : 'ยืนยันการรับสินค้า & เพิ่มสต๊อก' }}
          </button>
        </div>
        
        <div class="p-6">
          <!-- PO Reference Info -->
          <div [ngClass]="order.status === 'Pending' ? 'bg-emerald-50 border-emerald-200' : 'bg-slate-50 border-slate-200'" class="border p-4 rounded-lg mb-6 flex justify-between items-center">
            <div>
              <div [ngClass]="order.status === 'Pending' ? 'text-emerald-800' : 'text-slate-500'" class="text-xs font-bold uppercase mb-1">ดึงข้อมูลอ้างอิงจากใบสั่งซื้อ</div>
              <div class="text-xl font-bold text-slate-800">{{ order.poNumber }}</div>
              <div class="text-sm text-slate-600 mt-1">ซัพพลายเออร์: <span class="font-medium">{{ order.supplierName }}</span></div>
            </div>
            <div class="text-right">
              <div class="text-sm text-slate-600">วันที่สั่งซื้อ: {{ order.orderDate | date:'dd-MMM-yyyy' }}</div>
              <div class="text-xs px-2 py-1 rounded inline-block mt-2 font-bold"
                   [ngClass]="{
                     'bg-yellow-200 text-yellow-800': order.status === 'Pending',
                     'bg-green-200 text-green-800': order.status === 'Received',
                     'bg-red-200 text-red-800': order.status === 'Cancelled'
                   }">
                สถานะ: {{ order.status === 'Pending' ? 'รอรับสินค้า' : order.status }}
              </div>
            </div>
          </div>

          <h3 class="font-bold text-slate-700 mb-3 border-b pb-2">
            {{ order.status === 'Pending' ? 'ตรวจสอบรายการสินค้าที่ได้รับ' : 'รายการสินค้า' }}
          </h3>
          <p *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" class="text-sm text-gray-500 mb-4 no-print">
            กรุณาตรวจสอบจำนวนสินค้าที่ได้รับจริงเทียบกับจำนวนที่สั่งซื้อ หากได้รับไม่ครบสามารถแก้ไขที่ช่อง <strong>"จำนวนที่รับจริง"</strong> เพื่อให้ระบบอัปเดตสต๊อก (OnHand) ได้อย่างถูกต้องแม่นยำ
          </p>

          <div class="overflow-x-auto border rounded-lg">
            <table class="min-w-full divide-y divide-slate-200">
              <thead class="bg-slate-100 text-xs text-slate-600 uppercase font-bold border-b-2 border-slate-200">
                <tr>
                  <th class="px-4 py-3 text-left">รายละเอียดสินค้า</th>
                  <th class="px-4 py-3 text-right">ราคา/หน่วย (฿)</th>
                  <th class="px-4 py-3 text-center w-32 bg-slate-50">จำนวนที่สั่ง</th>
                  <th *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" class="px-4 py-3 text-center w-40 bg-emerald-50 text-emerald-800 border-l border-emerald-200">📦 จำนวนที่รับจริง</th>
                  <th *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" class="px-4 py-3 text-center w-32">หมายเหตุ</th>
                  <th *ngIf="order.status !== 'Pending' || !hasRole(['Admin', 'Warehouse'])" class="px-4 py-3 text-right w-40">รวมเป็นเงิน (฿)</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr *ngFor="let item of order.items" class="hover:bg-slate-50 transition-colors">
                  <td class="px-4 py-4">
                    <div class="font-bold text-slate-800">{{ item.productName }}</div>
                  </td>
                  <td class="px-4 py-4 text-right text-slate-600">
                    {{ item.unitCost | number:'1.2-2' }}
                  </td>
                  <td class="px-4 py-4 text-center font-bold text-slate-700 bg-slate-50">{{ item.quantity }}</td>
                  
                  <!-- Editable GR Column (Only when Pending and has role) -->
                  <td *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" class="px-4 py-3 bg-emerald-50 border-l border-emerald-200 align-middle">
                    <input type="number" [(ngModel)]="item.actualReceivedQty" min="0" [max]="item.quantity"
                           class="w-full text-center font-bold text-lg border-2 rounded py-1 focus:outline-none transition-colors"
                           [ngClass]="item.actualReceivedQty < item.quantity ? 'border-yellow-400 bg-yellow-50 text-yellow-700 focus:border-yellow-600' : 'border-emerald-400 focus:border-emerald-600'">
                  </td>
                  
                  <!-- Note Column -->
                  <td *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" class="px-4 py-4 text-center align-middle">
                    <span *ngIf="item.actualReceivedQty === item.quantity" class="text-emerald-600 text-xs font-bold"><i class="fas fa-check"></i> รับครบ</span>
                    <span *ngIf="item.actualReceivedQty < item.quantity" class="text-yellow-600 text-xs font-bold"><i class="fas fa-exclamation-triangle"></i> ขาด ({{ item.actualReceivedQty - item.quantity }})</span>
                  </td>

                  <!-- Total Cost Column -->
                  <td *ngIf="order.status !== 'Pending' || !hasRole(['Admin', 'Warehouse'])" class="px-4 py-4 text-right font-medium text-slate-800">
                    {{ (item.quantity * item.unitCost) | number:'1.2-2' }}
                  </td>
                </tr>
              </tbody>
              <tfoot *ngIf="order.status !== 'Pending' || !hasRole(['Admin', 'Warehouse'])">
                <tr class="bg-slate-50 border-t-2 border-slate-200">
                  <td colspan="3" class="px-4 py-4 text-right font-bold text-slate-700">ยอดรวมทั้งสิ้น (Grand Total)</td>
                  <td class="px-4 py-4 text-right font-bold text-blue-600 text-lg">฿{{ getTotal() | number:'1.2-2' }}</td>
                </tr>
              </tfoot>
            </table>
          </div>
          
          <div *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Warehouse'])" class="mt-6 p-4 bg-blue-50 text-blue-800 text-sm rounded border border-blue-200 flex items-start no-print">
            <i class="fas fa-info-circle mr-3 mt-1 text-blue-500 text-lg"></i> 
            <div>
              <strong>เกี่ยวกับสต๊อก:</strong> ทันทีที่คุณกดปุ่ม <strong>"ยืนยันการรับสินค้า"</strong> ระบบจะนำตัวเลขในช่องสีเขียว (จำนวนที่รับจริง) ไปบวกเพิ่มในสต๊อกบนชั้นวางของบริษัท (OnHand) แบบเรียลไทม์
            </div>
          </div>

        </div>
      </div>
    </div>
  `
})
export class PurchaseOrderDetailComponent implements OnInit {
  order: any = null;
  companySettings: any = null;
  isLoading = true;
  isSubmitting = false;

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
    this.http.get('http://localhost:5243/api/Settings/company').subscribe({
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
    this.isLoading = true;
    this.http.get(`http://localhost:5243/api/PurchaseOrders/${id}`).subscribe({
      next: (data: any) => {
        // Initialize actualReceivedQty for Goods Receipt UI
        if (data && data.items) {
          data.items = data.items.map((i: any) => ({
            ...i,
            actualReceivedQty: i.quantity // Default to full amount
          }));
        }
        this.order = data;
        this.isLoading = false;
      },
      error: (err) => {
        alert('Failed to load order details');
        this.isLoading = false;
        this.router.navigate(['/admin/purchase-orders']);
      }
    });
  }

  getTotal(): number {
    if (!this.order || !this.order.items) return 0;
    return this.order.items.reduce((sum: number, item: any) => sum + (item.quantity * item.unitCost), 0);
  }

  cancelOrder() {
    if (confirm('Are you sure you want to cancel this Purchase Order?')) {
      this.http.put(`http://localhost:5243/api/PurchaseOrders/${this.order.id}/cancel`, {}).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadOrder(this.order.id);
        },
        error: (err) => alert(err.error?.message || 'Failed to cancel order')
      });
    }
  }

  receiveOrder() {
    if (confirm('ยืนยันการรับสินค้า? ระบบจะนำ "จำนวนที่รับจริง" ไปเพิ่มในสต๊อกทันที')) {
      this.isSubmitting = true;
      const grPayload = {
        purchaseOrderId: this.order.id,
        remarks: 'Received via Goods Receipt Flow',
        items: this.order.items.map((i: any) => ({
          productId: i.productId,
          quantity: i.actualReceivedQty // Fix: API expects 'quantity'
        }))
      };

      this.http.post('http://localhost:5243/api/GoodsReceipts', grPayload).subscribe({
        next: (res: any) => {
          alert(res.message || 'บันทึกรับสินค้าสำเร็จ!');
          this.isSubmitting = false;
          this.loadOrder(this.order.id); // Reload to show new status
        },
        error: (err) => {
          this.isSubmitting = false;
          alert(err.error?.message || 'Failed to receive order');
        }
      });
    }
  }

  printDocument() {
    window.print();
  }
}
