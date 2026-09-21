import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-purchase-order-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './purchase-order-list.component.html',
  styleUrl: './purchase-order-list.component.scss'
})
export class PurchaseOrderListComponent implements OnInit {
  private http = inject(HttpClient);
  
  purchaseOrders: any[] = [];
  isLoading = true;

  ngOnInit() {
    this.loadPOs();
  }

  loadPOs() {
    this.isLoading = true;
    this.http.get<any[]>('http://localhost:5243/api/PurchaseOrders').subscribe({
      next: (data) => {
        this.purchaseOrders = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

}
