import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-order-tracking',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './order-tracking.component.html'
})
export class OrderTrackingComponent implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  orderId: string | null = null;
  order: any = null;
  isLoading = true;
  isUploading = false;
  uploadSuccess = false;

  countdownText = '';
  isExpired = false;
  private timerInterval: any;

  ngOnInit() {
    this.orderId = this.route.snapshot.paramMap.get('id');
    if (this.orderId) {
      this.fetchOrderDetails();
    }
  }

  ngOnDestroy() {
    if (this.timerInterval) clearInterval(this.timerInterval);
  }

  fetchOrderDetails() {
    this.http.get<any>(`http://localhost:5243/api/Storefront/orders/${this.orderId}`, { withCredentials: true }).subscribe({
      next: (data) => {
        this.order = data;
        this.isLoading = false;
        
        if (this.order.status === 'Cancelled' || this.order.paymentStatus === 'Expired') {
            this.isExpired = true;
            this.countdownText = '00:00';
            if (this.timerInterval) clearInterval(this.timerInterval);
        } else if (this.order.expiresAt && this.order.paymentStatus === 'Pending') {
            this.startCountdown(new Date(this.order.expiresAt));
        } else {
            if (this.timerInterval) clearInterval(this.timerInterval);
        }
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  startCountdown(expiresAt: Date) {
    if (this.timerInterval) clearInterval(this.timerInterval);

    this.timerInterval = setInterval(() => {
      const now = new Date().getTime();
      const expiry = expiresAt.getTime();
      const distance = expiry - now;

      if (distance <= 0) {
        clearInterval(this.timerInterval);
        this.isExpired = true;
        this.countdownText = '00:00';
        this.fetchOrderDetails();
      } else {
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);
        this.countdownText = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
      }
    }, 1000);
  }

  onFileSelected(event: any) {
    if (this.isExpired || this.order?.status === 'Cancelled') return;

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
        this.fetchOrderDetails(); 
      },
      error: (err) => {
        this.isUploading = false;
        alert(err.error?.message || 'Upload failed');
        if (err.error?.message?.toLowerCase().includes('expired') || err.error?.message?.toLowerCase().includes('cancelled')) {
          this.fetchOrderDetails();
        }
      }
    });
  }
}