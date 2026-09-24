import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-sales-order-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PaginationComponent],
  templateUrl: './sales-order-list.component.html',
  styleUrl: './sales-order-list.component.scss'
})
export class SalesOrderListComponent implements OnInit {
  private http = inject(HttpClient);
  
  salesOrders: any[] = [];
  isLoading = true;
  
  // Pagination State
  totalCount = 0;
  page = 1;
  pageSize = 10;
  searchQuery = '';
  statusFilter = ''; // 'Checking', 'Pending', etc.
  searchTimeout: any;

  ngOnInit() {
    this.loadSOs();
  }

  loadSOs() {
    this.isLoading = true;
    this.http.get<any>(`http://localhost:5243/api/SalesOrders?page=${this.page}&pageSize=${this.pageSize}&search=${this.searchQuery}&status=${this.statusFilter}`).subscribe({
      next: (data) => {
        this.salesOrders = data.items;
        this.totalCount = data.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  onSearchChange() {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.page = 1;
      this.loadSOs();
    }, 500); // Debounce 500ms
  }

  onPageChange(newPage: number) {
    this.page = newPage;
    this.loadSOs();
  }

  setFilter(status: string) {
    this.statusFilter = status;
    this.page = 1;
    this.loadSOs();
  }

  exportExcel() {
    this.http.get('http://localhost:5243/api/SalesOrders/export', { responseType: 'blob', withCredentials: true }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `SalesOrders_${new Date().getTime()}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Export failed', err);
        alert('Failed to export Sales Orders');
      }
    });
  }
}
