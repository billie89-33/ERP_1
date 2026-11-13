import { Component, inject, OnInit, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { AiService } from '../../../core/services/ai.service';
import { ProductDto, CategoryDto, FilterOptionsDto, FilterState } from '../../../core/models/product.model';

@Component({
  selector: 'app-shop',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss'
})
export class ShopComponent implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private aiService = inject(AiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // State Signals (Strict Typing)
  products = signal<ProductDto[]>([]);
  categories = signal<CategoryDto[]>([]);
  filterOptions = signal<FilterOptionsDto>({ brands: [], availableSpecs: {}, minPrice: 0, maxPrice: 0 });
  
  isLoading = signal<boolean>(false);
  isMobileFilterOpen = signal<boolean>(false);
  isAiSemanticMode = signal<boolean>(false);
  totalPages = signal<number>(1);
  totalProducts = signal<number>(0);

  // Core Filter State
  filterState = signal<FilterState>({
    categoryId: null,
    categoryName: null,
    searchQuery: '',
    brands: [],
    minPrice: null,
    maxPrice: null,
    selectedSpecs: {},
    page: 1,
    limit: 12
  });

  // Allowed Filters Configuration to prevent sidebar clutter
  private allowedSpecsConfig: { [category: string]: string[] } = {
    'Notebook': ['processors', 'memory', 'storage', 'video graphics', 'display', 'screen size', 'operating system'],
    'Graphics Card': ['gpu series', 'gpu model', 'memory size'],
    'Monitor': ['resolution', 'refresh rate', 'panel type', 'screen size'],
    'Mouse': ['interface', 'sensor technology', 'color']
  };

  // Computed Values
  specKeys = computed(() => {
    const allKeys = Object.keys(this.filterOptions().availableSpecs);
    const catName = this.filterState().categoryName;
    
    if (!catName) return [];

    const allowed = this.allowedSpecsConfig[catName];
    if (!allowed) {
      return allKeys.filter(k => k.toLowerCase() === 'color' || k.toLowerCase() === 'brand');
    }

    return allKeys.filter(key => allowed.includes(key.toLowerCase()));
  });
  hasActiveFilters = computed(() => {
    const s = this.filterState();
    return !!s.categoryName || s.brands.length > 0 || Object.keys(s.selectedSpecs).length > 0 || this.isAiSemanticMode();
  });

  constructor() {
    // Reactive: Whenever filterState changes, fetch products
    effect(() => {
      const state = this.filterState();
      if (!this.isAiSemanticMode()) {
        this.fetchProducts(state);
      }
    }, { allowSignalWrites: true });
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadFilterOptions(null);

    // Read initial query params from URL
    this.route.queryParams.subscribe(params => {
      if (params['category']) {
        this.selectCategoryByName(params['category']);
      }
    });
  }

  // --- API Calls ---
  private fetchProducts(state: FilterState) {
    this.isLoading.set(true);
    this.productService.getProducts(state).subscribe({
      next: (res) => {
        this.products.set(res.data);
        this.totalPages.set(res.totalPages);
        this.totalProducts.set(res.total);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load products', err);
        this.isLoading.set(false);
      }
    });
  }

  runAiSemanticSearch() {
    const q = this.filterState().searchQuery.trim();
    if (!q) {
      this.isAiSemanticMode.set(false);
      this.fetchProducts(this.filterState());
      return;
    }

    this.isAiSemanticMode.set(true);
    this.isLoading.set(true);
    this.aiService.semanticSearch({ query: q, topK: 12, onlyInStock: false }).subscribe({
      next: (matches) => {
        const mapped: ProductDto[] = matches.map(m => ({
          id: m.id,
          sku: m.sku,
          name: m.name,
          brand: m.brand,
          modelName: m.modelName,
          description: m.description,
          price: m.price,
          cost: m.cost,
          onHandQuantity: m.onHandQuantity,
          reservedQuantity: m.reservedQuantity,
          availableQuantity: m.availableQuantity,
          tags: m.tags || [],
          status: 'ACTIVE',
          isFeatured: false,
          soldCount: 0,
          viewCount: 0,
          specifications: m.specifications,
          categoryId: m.categoryId,
          categoryName: m.categoryName,
          image: m.imageUrl ? { url: m.imageUrl, publicId: '' } : undefined
        }));
        this.products.set(mapped);
        this.totalProducts.set(mapped.length);
        this.totalPages.set(1);
        this.isLoading.set(false);
      },
      error: () => {
        this.isAiSemanticMode.set(false);
        this.fetchProducts(this.filterState());
      }
    });
  }

  private loadCategories() {
    this.productService.getCategories().subscribe(res => this.categories.set(res));
  }

  private loadFilterOptions(categoryName: string | null) {
    this.productService.getFilterOptions(categoryName).subscribe(res => {
      this.filterOptions.set(res);
    });
  }

  // --- Actions ---
  onSearchChange(query: string) {
    if (this.isAiSemanticMode() && !query.trim()) {
      this.isAiSemanticMode.set(false);
    }
    this.filterState.update(s => ({ ...s, searchQuery: query, page: 1 }));
  }

  selectCategoryByName(catName: string | null) {
    this.isAiSemanticMode.set(false);
    this.filterState.update(s => ({ 
      ...s, 
      categoryName: catName, 
      page: 1, 
      brands: [], // Reset filters on category change
      selectedSpecs: {},
      minPrice: null,
      maxPrice: null
    }));
    this.loadFilterOptions(catName); // Load specific specs for this category

    // Update URL quietly
    if (catName) {
      this.router.navigate([], { queryParams: { category: catName }, queryParamsHandling: 'merge' });
    } else {
      this.router.navigate([], { queryParams: { category: null }, queryParamsHandling: 'merge' });
    }
  }

  toggleBrand(brand: string) {
    this.isAiSemanticMode.set(false);
    this.filterState.update(s => {
      const newBrands = s.brands.includes(brand) 
        ? s.brands.filter(b => b !== brand) 
        : [...s.brands, brand];
      return { ...s, brands: newBrands, page: 1 };
    });
  }

  toggleSpec(specKey: string, value: string) {
    this.isAiSemanticMode.set(false);
    this.filterState.update(s => {
      const currentSelected = s.selectedSpecs[specKey] || [];
      const newSelected = currentSelected.includes(value)
        ? currentSelected.filter(v => v !== value)
        : [...currentSelected, value];
        
      const newSpecs = { ...s.selectedSpecs };
      if (newSelected.length > 0) {
        newSpecs[specKey] = newSelected;
      } else {
        delete newSpecs[specKey]; // Clean up empty arrays
      }
      return { ...s, selectedSpecs: newSpecs, page: 1 };
    });
  }

  applyPriceRange(min: number | null, max: number | null) {
    this.isAiSemanticMode.set(false);
    this.filterState.update(s => ({ ...s, minPrice: min, maxPrice: max, page: 1 }));
  }

  clearFilters() {
    this.isAiSemanticMode.set(false);
    this.selectCategoryByName(null);
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.filterState.update(s => ({ ...s, page }));
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  toggleMobileFilter() {
    this.isMobileFilterOpen.update(val => !val);
  }

  // --- UI Helpers ---
  getSpecPreview(specs: Record<string, unknown> | null | undefined): string[] {
    if (!specs) return [];
    
    const targetSpecs = (specs['specifications'] as Record<string, unknown>) || specs;
    
    const ignoredKeys = ['__v', 'tags', 'brand', 'brands', 'image', 'status', 'createdAt', 'updatedAt', 'category', 'description', 'modelName', 'isFeatured', 'soldCount', 'viewCount'];
    
    const validKeys = Object.keys(targetSpecs)
      .filter(key => !ignoredKeys.includes(key) && !key.startsWith('_') && typeof targetSpecs[key] !== 'object');
      
    return validKeys.slice(0, 3).map(key => `${key}: ${targetSpecs[key]}`);
  }

  addToCart(product: ProductDto) {
    this.cartService.addToCart(product, 1);
  }
}
