import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { StorefrontAuthService } from '../../core/services/storefront-auth.service';
import { AiChatWidgetComponent } from '../../shared/components/ai-chat-widget/ai-chat-widget.component';

@Component({
  selector: 'app-storefront-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, AiChatWidgetComponent],
  templateUrl: './storefront-layout.component.html',
  styleUrl: './storefront-layout.component.scss'
})
export class StorefrontLayoutComponent {
  cartService = inject(CartService);
  authService = inject(StorefrontAuthService);
}
