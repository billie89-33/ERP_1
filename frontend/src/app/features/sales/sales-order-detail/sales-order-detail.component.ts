import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-sales-order-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `

<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8" *ngIf="!isLoading && order">
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">ออเดอร์: {{ order.orderNumber }}</h1>
        <p class="text-sm text-slate-500 mt-1">วันที่สั่งซื้อ: {{ order.orderDate | date:'medium' }}</p>
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
        <div *ngIf="order.status === 'Shipped' && order.trackingNumber" class="mt-3 p-4 bg-blue-50 border border-blue-200 rounded-lg max-w-sm">
          <p class="text-sm font-bold text-blue-900 mb-2"><i class="fas fa-truck mr-2"></i>ข้อมูลการจัดส่ง</p>
          <p class="text-sm text-blue-800 mb-1">ขนส่ง: <strong>{{ order.courier || '-' }}</strong></p>
          <p class="text-sm text-blue-800">เลขพัสดุ: <strong>{{ order.trackingNumber }}</strong></p>
        </div>
      </div>
      <div class="flex flex-wrap gap-3 no-print">
        <a routerLink="/admin/sales-orders" class="px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-700 rounded-md shadow-sm font-medium"><i class="fas fa-arrow-left mr-2"></i> กลับหน้ารวม</a>
        <button (click)="printDocument()" class="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-md shadow-sm font-medium"><i class="fas fa-print mr-2"></i> พิมพ์เอกสาร</button>
        
        <!-- Only Admin or Warehouse can ship (if paid) -->
        <button *ngIf="order.status === 'Pending' && order.paymentStatus === 'Paid' && hasRole(['Admin', 'Warehouse'])" (click)="openShippingModal()" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md shadow-sm font-medium"><i class="fas fa-box-open mr-2"></i> จัดส่งสินค้า (ตัดสต๊อก)</button>
        
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
      <div *ngIf="order.paymentStatus === 'Checking' || (order.paymentStatus === 'Paid' && order.paymentSlipUrl)" 
           class="p-6 border-b flex flex-col md:flex-row gap-8 items-start md:items-center no-print"
           [ngClass]="{'bg-amber-50 border-amber-200': order.paymentStatus === 'Checking', 'bg-green-50 border-green-200': order.paymentStatus === 'Paid'}">
        <div class="flex-shrink-0">
          <h3 class="text-base font-bold mb-3" [ngClass]="{'text-amber-900': order.paymentStatus === 'Checking', 'text-green-900': order.paymentStatus === 'Paid'}">
            <i class="fas" [ngClass]="{'fa-file-invoice-dollar': order.paymentStatus === 'Checking', 'fa-check-circle': order.paymentStatus === 'Paid'}"></i> 
            {{ order.paymentStatus === 'Checking' ? 'ตรวจสอบสลิปโอนเงิน' : 'สลิปโอนเงิน (ตรวจสอบแล้ว)' }}
          </h3>
          <div *ngIf="order.paymentSlipUrl; else noSlip">
            <a [href]="order.paymentSlipUrl" target="_blank" class="block cursor-pointer relative group">
              <img [src]="order.paymentSlipUrl" class="w-64 h-auto max-h-96 object-contain rounded-lg border-2 shadow-md transition-transform group-hover:scale-105" 
                   [ngClass]="{'border-amber-300': order.paymentStatus === 'Checking', 'border-green-300': order.paymentStatus === 'Paid'}" alt="รูปสลิปโอนเงิน">
              <div class="absolute inset-0 bg-black bg-opacity-40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity rounded-lg">
                <span class="text-white font-medium"><i class="fas fa-search-plus mr-2"></i>คลิกเพื่อดูรูปใหญ่</span>
              </div>
            </a>
          </div>
          <ng-template #noSlip>
            <div class="w-64 h-48 bg-slate-100 rounded-lg border-2 border-dashed border-slate-300 flex items-center justify-center text-slate-500 font-medium">
              ไม่พบรูปภาพสลิป
            </div>
          </ng-template>
        </div>
        
        <div class="flex-grow" *ngIf="order.paymentStatus === 'Checking'">
          <div class="bg-white p-5 rounded-lg border border-amber-200 shadow-sm">
            <h4 class="font-bold text-slate-800 mb-2">อนุมัติการชำระเงิน</h4>
            <p class="text-sm text-slate-600 mb-4">โปรดตรวจสอบรูปสลิปเทียบกับยอดที่ต้องชำระ <strong>฿{{ getTotal() | number:'1.2-2' }}</strong> ก่อนกดยืนยัน</p>
            
            <div class="flex gap-3" *ngIf="hasRole(['Admin', 'Sales'])">
              <button (click)="verifyPayment()" class="flex-1 px-4 py-3 bg-green-600 hover:bg-green-700 text-white font-bold rounded-lg shadow-sm transition-colors flex items-center justify-center">
                <i class="fas fa-check-circle mr-2"></i> อนุมัติ (รับยอดแล้ว)
              </button>
              <button (click)="rejectPayment()" class="flex-1 px-4 py-3 bg-red-100 hover:bg-red-200 text-red-700 font-bold rounded-lg shadow-sm transition-colors flex items-center justify-center border border-red-200">
                <i class="fas fa-times-circle mr-2"></i> ปฏิเสธ (สลิปไม่ถูกต้อง)
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-200 text-sm font-semibold text-slate-600">
              <th class="p-4">รูปภาพ</th>
              <th class="p-4">สินค้า</th>
              <th class="p-4 text-center">จำนวน</th>
              <th class="p-4 text-right">ราคา/หน่วย</th>
              <th class="p-4 text-right">รวม</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr *ngFor="let item of order.items" class="hover:bg-slate-50 transition-colors">
              <td class="p-4 w-20">
                <div class="h-12 w-12 rounded bg-slate-100 border border-slate-200 overflow-hidden flex items-center justify-center">
                  <img *ngIf="(item.productImage || item.productImageUrl)" [src]="(item.productImage || item.productImageUrl)" alt="{{item.productName}}" class="h-full w-full object-cover">
                  <i *ngIf="!(item.productImage || item.productImageUrl)" class="fas fa-box text-slate-300"></i>
                </div>
              </td>
              <td class="p-4 font-medium text-slate-800">{{ item.productName }}</td>
              <td class="p-4 text-center text-slate-600">{{ item.quantity }}</td>
              <td class="p-4 text-right text-slate-600">฿{{ item.unitPrice | number:'1.2-2' }}</td>
              <td class="p-4 text-right font-medium text-slate-800">฿{{ (item.quantity * item.unitPrice) | number:'1.2-2' }}</td>
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

    <!-- Shipping Modal -->
    <div *ngIf="showShippingModal" class="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
      <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" (click)="closeShippingModal()"></div>
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen">&#8203;</span>
        <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div class="sm:flex sm:items-start">
              <div class="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-blue-100 sm:mx-0 sm:h-10 sm:w-10">
                <i class="fas fa-truck text-blue-600"></i>
              </div>
              <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left w-full">
                <h3 class="text-lg leading-6 font-bold text-gray-900">ยืนยันการจัดส่งสินค้า</h3>
                <div class="mt-4 space-y-4">
                  <p class="text-sm text-gray-500">โปรดระบุบริษัทขนส่งและเลขพัสดุ (ถ้ามี) ข้อมูลนี้จะแสดงให้ลูกค้าเห็นในหน้าติดตามสถานะ</p>
                  <div>
                    <label class="block text-sm font-medium text-slate-700 mb-1 text-left">บริษัทขนส่ง</label>
                    <select [(ngModel)]="shippingCourier" class="w-full border-slate-300 rounded-md shadow-sm p-2 border">
                      <option value="">-- ไม่ระบุ --</option>
                      <option value="Kerry Express">Kerry Express</option>
                      <option value="Flash Express">Flash Express</option>
                      <option value="J&T Express">J&T Express</option>
                      <option value="Thailand Post">ไปรษณีย์ไทย (Thailand Post)</option>
                      <option value="Shopee Express">Shopee Express</option>
                      <option value="Other">อื่นๆ</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-slate-700 mb-1 text-left">เลขพัสดุ (Tracking Number)</label>
                    <input type="text" [(ngModel)]="trackingNumber" placeholder="เช่น TH123456789" class="w-full border-slate-300 rounded-md shadow-sm p-2 border">
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button type="button" (click)="confirmShipOrder()" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 sm:ml-3 sm:w-auto sm:text-sm">
              <i class="fas fa-check mr-2 mt-1"></i> ยืนยันจัดส่ง (ตัดสต๊อก)
            </button>
            <button type="button" (click)="closeShippingModal()" class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
              ยกเลิก
            </button>
          </div>
        </div>
      </div>
    </div>
</div>

`
})
export class SalesOrderDetailComponent implements OnInit {
  showShippingModal = false;
  shippingCourier = '';
  trackingNumber = '';
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

  openShippingModal() {
    this.shippingCourier = '';
    this.trackingNumber = '';
    this.showShippingModal = true;
  }

  closeShippingModal() {
    this.showShippingModal = false;
  }

  confirmShipOrder() {
    if (confirm('คุณแน่ใจหรือไม่ว่าต้องการจัดส่งสินค้านี้? (ระบบจะตัดสต๊อกทันที)')) {
      const giPayload = {
        salesOrderId: this.order.id,
        remarks: 'Shipped from SO Details',
        courier: this.shippingCourier,
        trackingNumber: this.trackingNumber,
        items: this.order.items.map((i: any) => ({
          productId: i.productId,
          quantity: i.quantity
        }))
      };

      this.http.post('/api/GoodsIssues', giPayload).subscribe({
        next: (res: any) => {
          alert(res.message);
          this.showShippingModal = false;
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
