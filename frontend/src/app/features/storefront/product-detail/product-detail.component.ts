import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { ProductDto } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss'
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private cartService = inject(CartService);

  // State Signals
  product = signal<ProductDto | null>(null);
  isLoading = signal<boolean>(true);
  error = signal<string>('');
  quantity = signal<number>(1);
  activeTab = signal<'description' | 'specs'>('specs');

  // Computed Values
  parsedProductInfo = computed(() => {
    const p = this.product();
    if (!p) return { name: '', model: '', description: '' };

    let rawName = p.name || '';
    
    let namePart = rawName;
    let descPart = '';
    
    // ข้อมูลจาก MongoDB ดันเอา Description ไปต่อท้าย Name (สังเกตจากหลังวงเล็บปิด)
    const descMatch = rawName.match(/(\)\s+)([\u0E00-\u0E7F].*)/);
    if (descMatch) {
        const splitIndex = rawName.indexOf(descMatch[2]);
        namePart = rawName.substring(0, splitIndex).trim();
        descPart = rawName.substring(splitIndex).trim();
    }

    // แยก Model (สิ่งที่อยู่ในวงเล็บท้ายสุด เช่น (PLATINUM GRAY) (2Y))
    let shortName = namePart;
    let model = '';
    const trailingModelRegex = /(\s+\(.*?\))+$/;
    const modelMatch = namePart.match(trailingModelRegex);
    if (modelMatch) {
       model = modelMatch[0].trim();
       shortName = namePart.replace(modelMatch[0], '').trim();
    }

    // Use description from top-level field if available, otherwise fallback to parsed descPart
    let finalDesc = p.description || descPart;
    
    // Sometimes top-level description is exact same as name, in that case use the split part if it exists
    if (finalDesc === rawName && descPart) {
        finalDesc = descPart;
    }
    
    return {
       name: shortName,
       model: model,
       description: finalDesc
    };
  });

  technicalSpecs = computed(() => {
    const p = this.product();
    if (!p || !p.specifications) return [];

    // Unwrap nested specifications if exists
    const targetSpecs = p.specifications['specifications'] || p.specifications;

    const ignoredKeys = ['__v', 'tags', 'brand', 'brands', 'image', 'status', 'createdAt', 'updatedAt', 'category', 'description', 'modelName', 'isFeatured', 'soldCount', 'viewCount'];

    const validKeys = Object.keys(targetSpecs)
      .filter(key => !ignoredKeys.includes(key) && !key.startsWith('_') && typeof targetSpecs[key] !== 'object');

    return validKeys.map(key => ({
      key,
      value: targetSpecs[key]
    }));
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadProduct(id);
    } else {
      this.error.set('Product ID is missing');
      this.isLoading.set(false);
    }
  }

  loadProduct(id: string) {
    this.productService.getProduct(id).subscribe({
      next: (res) => {
        this.product.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load product details.');
        this.isLoading.set(false);
      }
    });
  }

  increaseQty() {
    const p = this.product();
    if (p && this.quantity() < p.availableQuantity) {
      this.quantity.update(q => q + 1);
    }
  }

  decreaseQty() {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }

  addToCart() {
    const p = this.product();
    if (p) {
      this.cartService.addToCart(p, this.quantity());
      
      // Optional: show a tiny toast or just let the cart badge update
      // We will reset quantity to 1 after adding
      this.quantity.set(1);
    }
  }

  buyNow() {
    const p = this.product();
    if (p) {
      this.cartService.addToCart(p, this.quantity());
      this.router.navigate(['/cart']);
    }
  }
}
