import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-order-tracking',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './order-tracking.component.html'
})
export class OrderTrackingComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  orderId: string | null = null;
  order: any = null;
  isLoading = true;
  isUploading = false;
  uploadSuccess = false;

  ngOnInit() {
    this.orderId = this.route.snapshot.paramMap.get('id');
    if (this.orderId) {
      this.fetchOrderDetails();
    }
  }

  fetchOrderDetails() {
    this.http.get<any>(`http://localhost:5243/api/Storefront/orders/${this.orderId}`, { withCredentials: true }).subscribe({
      next: (data) => {
        this.order = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        const base64Image = e.target.result;
        this.uploadSlip(base64Image);
      };
      reader.readAsDataURL(file);
    }
  }

  uploadSlip(base64Image: string) {
    this.isUploading = true;
    this.uploadSuccess = false;
    this.http.post(`http://localhost:5243/api/Storefront/orders/${this.orderId}/upload-slip`, 
      { base64Image }, 
      { withCredentials: true }
    ).subscribe({
      next: () => {
        this.isUploading = false;
        this.uploadSuccess = true;
        this.fetchOrderDetails(); // Reload to show updated status
      },
      error: () => {
        this.isUploading = false;
        alert('เกิดข้อผิดพลาดในการอัปโหลดสลิป');
      }
    });
  }
}
