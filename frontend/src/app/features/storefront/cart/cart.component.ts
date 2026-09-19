import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent {
  cartService = inject(CartService);

  updateQuantity(productId: string, currentQty: number, delta: number) {
    const newQty = currentQty + delta;
    if (newQty > 0) {
      this.cartService.updateQuantity(productId, newQty);
    }
  }

  removeItem(productId: string) {
    this.cartService.removeFromCart(productId);
  }

  checkout() {
    alert('Checkout feature coming soon in Phase 5!');
  }
}
