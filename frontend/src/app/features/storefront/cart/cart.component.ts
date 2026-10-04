import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { CartService, CheckoutPayload } from '../../../core/services/cart.service';
import { StorefrontAuthService } from '../../../core/services/storefront-auth.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent implements OnInit {
  cartService = inject(CartService);
  private authService = inject(StorefrontAuthService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private http = inject(HttpClient);

  isCheckoutMode = false;
  isSubmitting = false;
  checkoutForm: FormGroup;

  constructor() {
    this.checkoutForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      address: ['', Validators.required]
    });
  }

  ngOnInit() {
    const customer = this.authService.currentUser();
    if (customer) {
      this.http.get<any>('/api/StorefrontAuth/profile', { withCredentials: true }).subscribe({
        next: (data) => {
          this.checkoutForm.patchValue({
            firstName: data.firstName || '',
            lastName: data.lastName || '',
            email: data.email || customer.email || '',
            phone: data.phone || '',
            address: data.address || ''
          });
        },
        error: (err) => console.error('Error loading profile for checkout', err)
      });
    }
  }

  updateQuantity(productId: string, currentQty: number, delta: number) {
    const newQty = currentQty + delta;
    if (newQty > 0) {
      this.cartService.updateQuantity(productId, newQty);
    }
  }

  removeItem(productId: string) {
    this.cartService.removeFromCart(productId);
  }

  startCheckout() {
    if (!this.authService.currentUser()) {
      alert('กรุณาเข้าสู่ระบบก่อนทำการสั่งซื้อครับ');
      this.router.navigate(['/shop/login'], { queryParams: { returnUrl: '/cart' } });
      return;
    }
    this.isCheckoutMode = true;
  }

  cancelCheckout() {
    this.isCheckoutMode = false;
  }

  async submitOrder() {
    if (this.checkoutForm.invalid) {
      this.checkoutForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    try {
      const formValue = this.checkoutForm.value;
      const payload: CheckoutPayload = {
        firstName: formValue.firstName,
        lastName: formValue.lastName,
        email: formValue.email,
        phone: formValue.phone,
        address: formValue.address,
        items: this.cartService.items().map(i => ({
          productId: i.productId,
          quantity: i.quantity
        }))
      };

      const response: any = await this.cartService.submitCheckout(payload);
      
      // Navigate to Payment / Order Tracking page instead of home!
      if (response && response.orderId) {
        this.router.navigate(['/shop/orders', response.orderId]);
      } else {
        alert(`Checkout successful! Order Number: ${response.orderNumber}`);
        this.router.navigate(['/']);
      }
    } catch (error: any) {
      alert(`Checkout failed: ${error.error?.error || error.error?.message || error.message}`);
    } finally {
      this.isSubmitting = false;
    }
  }
}
