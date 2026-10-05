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
    <div *ngIf="isLoading" class="p-8 text-center text-slate-500">
      <i class="fas fa-spinner fa-spin fa-2x"></i>
      <p class="mt-2">กำลังโหลดข้อมูล...</p>
    </div>

    <div *ngIf="!isLoading && order" class="max-w-5xl mx-auto space-y-6">
      
      <!-- Company Header for Printing -->
      <div class="hidden print:block text-center border-b pb-6 mb-6">
        <h1 class="text-3xl font-bold">{{ companySettings?.companyName || "บริษัท จำกัด" }}</h1>
        <p class="text-slate-600 mt-2">{{ companySettings?.address }}</p>
        <p class="text-slate-600">เลขประจำตัวผู้เสียภาษี: {{ companySettings?.taxId }} | โทร: {{ companySettings?.phone }}</p>
        <br>
        <h2 class="text-xl font-bold uppercase tracking-wider">ใบเสร็จรับเงิน / ใบส่งสินค้า</h2>
      </div>

      <div class="mb-6 flex flex-col md:flex-row justify-between items-start md:items-center gap-4 no-print">
        <div>
          <h1 class="text-2xl font-bold text-slate-800">ออเดอร์: {{ order.orderNumber }}</h1>
          <p class="text-slate-500 text-sm mt-1">วันที่สั่งซื้อ: {{ order.orderDate | date: 'medium' }}</p>
          <div class="flex gap-2 mt-2">
            <span class="px-3 py-1 rounded-full text-xs font-semibold"
                  [ngClass]="{
                    'bg-yellow-100 text-yellow-800': order.status === 'Pending',
                    'bg-green-100 text-green-800': order.status === 'Shipped',
                    'bg-red-100 text-red-800': order.status === 'Cancelled'
                  }">
              สถานะ: {{ order.status }}
            </span>
            <span class="px-3 py-1 rounded-full text-xs font-semibold"
               [ngClass]="{
                 'bg-red-100 text-red-800': order.paymentStatus === 'Pending',
                 'bg-yellow-100 text-yellow-800': order.paymentStatus === 'Checking',
                 'bg-green-100 text-green-800': order.paymentStatus === 'Paid'
               }">
              การชำระเงิน: {{ order.paymentStatus || 'Pending' }}
            </span>
          </div>
        </div>
        <div class="flex flex-wrap gap-3 no-print">
          <a routerLink="/admin/sales-orders" class="px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-700 rounded-md shadow-sm font-medium"><i class="fas fa-arrow-left mr-2"></i> กลับหน้ารวม</a>
          <button (click)="printDocument()" class="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-md shadow-sm font-medium"><i class="fas fa-print mr-2"></i> พิมพ์เอกสาร</button>
          
          <!-- Only Admin or Warehouse can ship (if paid) -->
          <button *ngIf="order.status === 'Pending' && order.paymentStatus === 'Paid' && hasRole(['Admin', 'Warehouse'])" (click)="shipOrder()" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md shadow-sm font-medium"><i class="fas fa-box-open mr-2"></i> จัดส่งสินค้า (ตัดสต๊อก)</button>
          
          <!-- Only Admin or Sales can cancel -->
          <button *ngIf="order.status === 'Pending' && hasRole(['Admin', 'Sales'])" (click)="cancelOrder()" class="px-4 py-2 bg-red-100 hover:bg-red-200 text-red-700 rounded-md shadow-sm font-medium border border-red-200"><i class="fas fa-times mr-2"></i> ยกเลิกออเดอร์</button>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow overflow-hidden mb-6">
        <div class="p-6 border-b border-slate-200 flex flex-col md:flex-row justify-between items-start gap-4">
          <div>
            <h3 class="text-sm font-medium text-slate-500 uppercase">ข้อมูลลูกค้า</h3>
            <p class="text-lg font-semibold text-slate-800 mt-1">{{ order.customerName }}</p>
            <p class="text-sm text-slate-500" *ngIf="order.customerEmail"><i class="fas fa-envelope mr-2"></i>{{ order.customerEmail }}</p>
          </div>
        </div>
        
        <!-- Payment Slip Verification Section -->
        <div *ngIf="order.paymentStatus === 'Checking'" class="p-6 bg-amber-50 border-b border-amber-200 flex flex-col md:flex-row gap-8 items-start md:items-center no-print">
          <div class="flex-shrink-0">
            <h3 class="text-base font-bold text-amber-900 mb-3"><i class="fas fa-file-invoice-dollar mr-2"></i>ตรวจสอบหลักฐานการโอนเงิน</h3>
            <div *ngIf="order.paymentSlipUrl; else noSlip">
              <a [href]="order.paymentSlipUrl" target="_blank" class="block cursor-pointer relative group">
                <img [src]="order.paymentSlipUrl" class="w-64 h-auto max-h-96 object-contain rounded-lg border-2 border-amber-300 shadow-md transition-transform group-hover:scale-105" alt="สลิปโอนเงิน">
                <div class="absolute inset-0 bg-black bg-opacity-40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity rounded-lg">
                  <span class="text-white font-medium"><i class="fas fa-search-plus mr-2"></i>คลิกเพื่อดูรูปใหญ่</span>
                </div>
              </a>
            </div>
            <ng-template #noSlip>
              <div class="w-64 h-48 bg-amber-100 rounded-lg border-2 border-dashed border-amber-300 flex items-center justify-center text-amber-700 font-medium">
                ไม่พบรูปภาพสลิป
              </div>
            </ng-template>
          </div>
          
          <div class="flex-grow">
            <div class="bg-white p-5 rounded-lg border border-amber-200 shadow-sm">
              <h4 class="font-bold text-slate-800 mb-2">การตัดสินใจ</h4>
              <p class="text-sm text-slate-600 mb-4">โปรดตรวจสอบยอดเงินเข้าบัญชีให้ตรงกับยอดชำระ <strong>฿{{ getTotal() | number:'1.2-2' }}</strong> ก่อนทำการอนุมัติ</p>
              
              <div class="flex gap-3" *ngIf="hasRole(['Admin', 'Sales'])">
                <button (click)="verifyPayment()" class="flex-1 px-4 py-3 bg-green-600 hover:bg-green-700 text-white font-bold rounded-lg shadow-sm transition-colors flex items-center justify-center">
                  <i class="fas fa-check-circle mr-2"></i> อนุมัติ (รับยอดแล้ว)
                </button>
                <button (click)="rejectPayment()" class="flex-1 px-4 py-3 bg-red-100 hover:bg-red-200 text-red-700 font-bold rounded-lg shadow-sm border border-red-200 transition-colors flex items-center justify-center">
                  <i class="fas fa-times-circle mr-2"></i> ปฏิเสธ (สลิปไม่ถูกต้อง)
                </button>
              </div>
              <div *ngIf="!hasRole(['Admin', 'Sales'])" class="text-sm text-amber-600 font-medium">
                * คุณไม่มีสิทธิ์ในการอนุมัติการชำระเงิน
              </div>
            </div>
          </div>
        </div>

        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 text-slate-500 text-xs uppercase tracking-wider">
              <th class="p-4 font-medium border-b border-slate-200 w-24 text-center">รูปภาพ</th>
              <th class="p-4 font-medium border-b border-slate-200">สินค้า</th>
              <th class="p-4 font-medium border-b border-slate-200 text-center">จำนวน</th>
              <th class="p-4 font-medium border-b border-slate-200 text-right">ราคา/หน่วย</th>
              <th class="p-4 font-medium border-b border-slate-200 text-right">รวม</th>
            </tr>
          </thead>
          <tbody class="text-sm divide-y divide-slate-100">
            <tr *ngFor="let item of order.items" class="hover:bg-slate-50 transition-colors">
              <td class="p-4 text-center">
                <div class="w-16 h-16 bg-white border border-slate-200 rounded-md overflow-hidden mx-auto flex items-center justify-center">
                  <img *ngIf="item.productImage; else noImg" [src]="item.productImage" alt="{{ item.productName }}" class="w-full h-full object-cover">
                  <ng-template #noImg><i class="fas fa-box text-slate-300 text-2xl"></i></ng-template>
                </div>
              </td>
              <td class="p-4 text-slate-800 font-medium">{{ item.productName }}</td>
              <td class="p-4 text-slate-600 text-center">{{ item.quantity }}</td>
              <td class="p-4 text-slate-600 text-right">฿{{ item.unitPrice | number:'1.2-2' }}</td>
              <td class="p-4 text-slate-800 font-semibold text-right">฿{{ (item.quantity * item.unitPrice) | number:'1.2-2' }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="bg-slate-50">
              <td colspan="4" class="p-4 text-right font-bold text-slate-700">ยอดสุทธิ (Grand Total)</td>
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
    if (confirm('คุณแน่ใจหรือไม่ว่าต้องการยกเลิกออเดอร์นี้? (ระบบจะคืนสต๊อกที่จองไว้ทั้งหมด)')) {
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
    if (confirm('คุณแน่ใจหรือไม่ว่าต้องการจัดส่งสินค้านี้? (ระบบจะตัดสต๊อกทันที)')) {
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
    if (confirm('คุณแน่ใจหรือไม่ว่ายอดเงินเข้าบัญชีถูกต้องแล้ว? (กดตกลงเพื่ออนุมัติ)')) {
      this.http.post(`/api/SalesOrders/${this.order.id}/verify-payment`, {}, { withCredentials: true }).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadOrder(this.order.id);
        },
        error: (err) => alert(err.error?.message || 'Failed to verify payment')
      });
    }
  }

  rejectPayment() {
    if (confirm('คุณแน่ใจหรือไม่ว่าต้องการปฏิเสธสลิปนี้? ระบบจะแจ้งให้ลูกค้าอัปโหลดสลิปใหม่')) {
      this.http.post(`/api/SalesOrders/${this.order.id}/reject-payment`, {}, { withCredentials: true }).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.loadOrder(this.order.id);
        },
        error: (err) => alert(err.error?.message || 'Failed to reject payment')
      });
    }
  }

  printDocument() {
    window.print();
  }
}
