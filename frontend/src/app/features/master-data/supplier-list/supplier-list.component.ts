import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-supplier-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PaginationComponent],
  template: `
    <div class="mb-6 flex justify-between items-center">
      <div>
        <h1 class="text-2xl font-bold text-slate-800">ฐานข้อมูลซัพพลายเออร์ (Suppliers)</h1>
        <p class="text-sm text-slate-500 mt-1">Master Data สำหรับข้อมูลบริษัทคู่ค้า / ผู้ขาย</p>
      </div>
      <a routerLink="/admin/suppliers/create" class="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded shadow-sm transition flex items-center">
        <i class="fas fa-plus mr-2"></i> เพิ่มซัพพลายเออร์
      </a>
    </div>

    <div class="bg-white shadow-md rounded-lg overflow-hidden border border-slate-200">
      <div class="p-4 border-b border-slate-200 bg-slate-50 flex justify-between items-center">
        <div class="relative w-64">
          <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <i class="fas fa-search text-slate-400"></i>
          </div>
          <input type="text" [(ngModel)]="searchQuery" (ngModelChange)="onSearchChange()" placeholder="Search company, tax id..." class="pl-10 focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-slate-300 rounded-md">
        </div>
      </div>
      
      <div *ngIf="isLoading" class="p-12 text-center text-slate-500">
        <i class="fas fa-spinner fa-spin text-3xl mb-3 text-blue-500"></i>
        <p>กำลังโหลดข้อมูลซัพพลายเออร์...</p>
      </div>
      
      <div *ngIf="!isLoading" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-slate-200 text-sm">
          <thead class="bg-slate-100">
            <tr>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">ชื่อบริษัท (Company Name)</th>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">เลขผู้เสียภาษี (Tax ID)</th>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">ชื่อผู้ติดต่อ</th>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">เบอร์โทรศัพท์</th>
              <th class="px-6 py-3 text-right font-bold text-slate-600 uppercase">การจัดการ</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-slate-200">
            <tr *ngFor="let sup of suppliers" class="hover:bg-slate-50 transition-colors">
              <td class="px-6 py-4">
                <div class="font-bold text-slate-800">{{ sup.companyName }}</div>
              </td>
              <td class="px-6 py-4 text-slate-600">{{ sup.taxId || '-' }}</td>
              <td class="px-6 py-4 text-slate-600">{{ sup.contactName || '-' }}</td>
              <td class="px-6 py-4 text-slate-600">{{ sup.phone || '-' }}</td>
              <td class="px-6 py-4 text-right">
                <a [routerLink]="['/admin/suppliers/edit', sup.id]" class="text-blue-600 hover:text-blue-900 font-medium">แก้ไข</a>
              </td>
            </tr>
            <tr *ngIf="suppliers.length === 0">
              <td colspan="5" class="px-6 py-12 text-center text-slate-500">
                <i class="fas fa-building text-4xl mb-3 text-slate-300"></i>
                <p>ยังไม่มีข้อมูลซัพพลายเออร์ในระบบ</p>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <app-pagination 
        *ngIf="!isLoading && totalCount > 0"
        [totalCount]="totalCount" 
        [pageSize]="pageSize" 
        [currentPage]="page" 
        (pageChange)="onPageChange($event)">
      </app-pagination>
    </div>
  `
})
export class SupplierListComponent implements OnInit {
  suppliers: any[] = [];
  isLoading = true;

  totalCount = 0;
  page = 1;
  pageSize = 10;
  searchQuery = '';
  searchTimeout: any;

  private http = inject(HttpClient);

  ngOnInit() {
    this.loadSuppliers();
  }

  loadSuppliers() {
    this.isLoading = true;
    this.http.get<any>(`/api/Suppliers?page=${this.page}&pageSize=${this.pageSize}&search=${this.searchQuery}`).subscribe({
      next: (data) => {
        this.suppliers = data.items;
        this.totalCount = data.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load suppliers', err);
        this.isLoading = false;
      }
    });
  }

  onSearchChange() {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.page = 1;
      this.loadSuppliers();
    }, 500);
  }

  onPageChange(newPage: number) {
    this.page = newPage;
    this.loadSuppliers();
  }
}
