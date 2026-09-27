import { Component, ElementRef, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html'
})
export class HomeComponent {
  @ViewChild('scrollContainer') scrollContainer!: ElementRef;

  newProducts = [
    {
      id: 1,
      name: 'VGA(การ์ดจอ) COLORFUL GEFORCE RTX 5060 TI GAMING DUO V2-V - 16GB...',
      price: 27490.00,
      image: 'https://placehold.co/400x300/1e293b/ffffff?text=RTX+5060+Ti'
    },
    {
      id: 2,
      name: 'VGA(การ์ดจอ) ASUS T1 GEFORCE RTX 5070 OC EDITION - 12GB GDDR7...',
      price: 37300.00,
      image: 'https://placehold.co/400x300/1e293b/ffffff?text=RTX+5070'
    },
    {
      id: 3,
      name: 'VGA(การ์ดจอ) ZOTAC GAMING GEFORCE RTX 5060 TI MOON WHITE O...',
      price: 20990.00,
      image: 'https://placehold.co/400x300/1e293b/ffffff?text=RTX+5060'
    },
    {
      id: 4,
      name: 'MAINBOARD (เมนบอร์ด) (AM5) GIGABYTE X870 AORUS ELITE WIFI7...',
      price: 9690.00,
      image: 'https://placehold.co/400x300/1e293b/ffffff?text=X870+AORUS'
    },
    {
      id: 5,
      name: 'MAINBOARD (เมนบอร์ด) (1851) GIGABYTE Z890 AORUS ELITE WIFI7 PLU...',
      price: 8490.00,
      image: 'https://placehold.co/400x300/1e293b/ffffff?text=Z890+AORUS'
    },
    {
      id: 6,
      name: 'MAINBOARD (เมนบอร์ด) (AM5) ASROCK B850 ROCK WIFI 7 (3Y)',
      price: 5990.00,
      image: 'https://placehold.co/400x300/1e293b/ffffff?text=B850+ROCK'
    }
  ];

  scrollLeft() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollBy({ left: -300, behavior: 'smooth' });
    }
  }

  scrollRight() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollBy({ left: 300, behavior: 'smooth' });
    }
  }
}

