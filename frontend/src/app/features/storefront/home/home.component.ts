import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { CompanySettingsDto } from '../../../core/models/erp.model';
import { CategoryDto, ProductDto } from '../../../core/models/product.model';

interface CategoryVisualMeta {
  icon: string;
  subtitle: string;
  badgeColor: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  @ViewChild('scrollContainer') scrollContainer?: ElementRef<HTMLDivElement>;

  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private http = inject(HttpClient);

  categories: CategoryDto[] = [];
  newProducts: ProductDto[] = [];
  popularProducts: ProductDto[] = [];

  isLoadingNew = true;
  isLoadingPopular = true;

  heroBadge = 'New Generation Components';
  heroTitle = 'Jamine CPU คอมพิวเตอร์เซตสเปกแรง ตอบโจทย์ทุกการใช้งาน';
  heroSubtitle =
    'คัดสรรอุปกรณ์ไอทีคุณภาพ ประกันศูนย์แท้ 100% ประกอบและเทสระบบก่อนจัดส่ง พร้อมผู้ช่วย AI แนะนำสเปคตามงบประมาณ';
  heroImageUrl = 'hero-computer.png';
  heroCtaText = 'เลือกซื้อสินค้าเลย';

  ngOnInit(): void {
    this.loadHeroSettings();
    this.loadStorefrontData();
  }

  private loadHeroSettings(): void {
    this.http
      .get<CompanySettingsDto>('/api/Settings/company')
      .pipe(catchError(() => of(null)))
      .subscribe((settings) => {
        if (!settings) return;
        if (settings.heroBadge?.trim()) this.heroBadge = settings.heroBadge;
        if (settings.heroTitle?.trim()) this.heroTitle = settings.heroTitle;
        if (settings.heroSubtitle?.trim()) this.heroSubtitle = settings.heroSubtitle;
        if (settings.heroImageUrl?.trim()) this.heroImageUrl = settings.heroImageUrl;
        if (settings.heroCtaText?.trim()) this.heroCtaText = settings.heroCtaText;
      });
  }

  private loadStorefrontData(): void {
    forkJoin({
      categories: this.productService.getCategories().pipe(catchError(() => of([] as CategoryDto[]))),
      newest: this.productService
        .getProducts({ page: 1, limit: 10, sortBy: 'newest', onlyActive: true })
        .pipe(catchError(() => of(null))),
      popular: this.productService
        .getProducts({ page: 1, limit: 8, sortBy: 'popular', onlyActive: true })
        .pipe(catchError(() => of(null)))
    }).subscribe(({ categories, newest, popular }) => {
      this.categories = categories;
      this.newProducts = newest?.data ?? [];
      this.popularProducts = popular?.data ?? [];
      this.isLoadingNew = false;
      this.isLoadingPopular = false;
    });
  }

  getCategoryMeta(categoryName: string): CategoryVisualMeta {
    const key = (categoryName || '').trim().toLowerCase();
    if (key.includes('notebook') || key.includes('laptop')) {
      return { icon: '💻', subtitle: 'โน้ตบุ๊กทำงาน & เกมมิ่ง', badgeColor: 'from-blue-500/20 to-indigo-500/20 border-blue-500/30' };
    }
    if (key.includes('graphic') || key.includes('gpu') || key.includes('vga')) {
      return { icon: '🎮', subtitle: 'การ์ดจอ RTX ซีรีส์ล่าสุด', badgeColor: 'from-purple-500/20 to-pink-500/20 border-purple-500/30' };
    }
    if (key.includes('monitor') || key.includes('display')) {
      return { icon: '🖥️', subtitle: 'จอมอนิเตอร์ 100Hz - 180Hz', badgeColor: 'from-cyan-500/20 to-teal-500/20 border-cyan-500/30' };
    }
    if (key.includes('mouse')) {
      return { icon: '🖱️', subtitle: 'เมาส์ไร้สาย & เกมมิ่ง', badgeColor: 'from-emerald-500/20 to-green-500/20 border-emerald-500/30' };
    }
    return { icon: '⚡', subtitle: 'อุปกรณ์ไอทีคุณภาพ', badgeColor: 'from-slate-700/40 to-slate-800/40 border-slate-600/40' };
  }

  scrollLeft(): void {
    this.scrollContainer?.nativeElement.scrollBy({ left: -300, behavior: 'smooth' });
  }

  scrollRight(): void {
    this.scrollContainer?.nativeElement.scrollBy({ left: 300, behavior: 'smooth' });
  }

  addToCart(event: Event, product: ProductDto): void {
    event.preventDefault();
    event.stopPropagation();
    if (product.availableQuantity <= 0) {
      alert('ขออภัย สินค้านี้หมดชั่วคราวครับ');
      return;
    }
    this.cartService.addToCart(product, 1);
    alert(`${product.name} ถูกเพิ่มลงตะกร้าแล้ว!`);
  }
}
