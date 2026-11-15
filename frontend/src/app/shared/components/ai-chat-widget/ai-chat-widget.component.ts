import { Component, ElementRef, Input, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AiService } from '../../../core/services/ai.service';
import { CartService } from '../../../core/services/cart.service';
import {
  ChatDocumentRefDto,
  ChatHistoryItemDto,
  SemanticProductMatchDto
} from '../../../core/models/ai.model';
import { ProductDto } from '../../../core/models/product.model';

export interface ChatUiMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: string;
  recommendedProducts?: SemanticProductMatchDto[];
  referencedDocuments?: ChatDocumentRefDto[];
}

@Component({
  selector: 'app-ai-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './ai-chat-widget.component.html'
})
export class AiChatWidgetComponent {
  @Input() mode: 'storefront' | 'admin' = 'storefront';
  @ViewChild('messagesContainer') private messagesContainer?: ElementRef<HTMLDivElement>;

  private aiService = inject(AiService);
  private cartService = inject(CartService);

  isOpen = signal<boolean>(false);
  isSending = signal<boolean>(false);
  inputMessage = '';

  messages = signal<ChatUiMessage[]>([]);

  get quickPrompts(): string[] {
    if (this.mode === 'admin') {
      return [
        'สรุปรายรับ SO และรายจ่าย PO ของร้านตอนนี้',
        'สินค้าไหนสต๊อกใกล้หมด ต้องเปิด PO สั่งเพิ่ม?',
        'บิลขาย (SO) ไหนรอตรวจสลิปหรือรอส่งของ (GI)?',
        'ซัพพลายเออร์และลูกค้ารายใหญ่สุดคือใคร?'
      ];
    }
    return [
      'แนะนำสินค้าคุ้มค่าในงบ 15,000 - 25,000 บาท',
      'มีอุปกรณ์สำหรับเล่นเกมหรือทำงานกราฟิกอะไรพร้อมส่งบ้าง?',
      'แนะนำสินค้าขายดีที่มีของในสต๊อกตอนนี้'
    ];
  }

  toggleChat(): void {
    const nextState = !this.isOpen();
    this.isOpen.set(nextState);

    if (nextState && this.messages().length === 0) {
      const greeting =
        this.mode === 'admin'
          ? 'สวัสดีครับเจ้านาย! ผมคือ JAMiNE ERP Copilot พร้อมช่วยวิเคราะห์สต๊อก รายรับ (SO) รายจ่าย (PO) ซัพพลายเออร์ ลูกค้า และงานคลังสินค้า (GR/GI) จากฐานข้อมูลจริงครับ'
          : 'สวัสดีครับ! ผมคือผู้ช่วย AI ประจำร้าน Jamine IT สอบถามสเปคสินค้า งบประมาณ หรือให้ช่วยเทียบความคุ้มค่าได้เลยครับ';

      this.messages.set([
        {
          role: 'assistant',
          content: greeting,
          timestamp: this.getCurrentTime()
        }
      ]);
    }

    if (nextState) {
      this.scrollToBottom();
    }
  }

  sendPrompt(promptText: string): void {
    this.inputMessage = promptText;
    this.sendMessage();
  }

  sendMessage(): void {
    const trimmed = this.inputMessage.trim();
    if (!trimmed || this.isSending()) return;

    const historyPayload: ChatHistoryItemDto[] = this.messages()
      .slice(-6)
      .map((m) => ({
        role: m.role === 'assistant' ? 'model' : 'user',
        text: m.content
      }));

    const userMsg: ChatUiMessage = {
      role: 'user',
      content: trimmed,
      timestamp: this.getCurrentTime()
    };

    this.messages.update((prev) => [...prev, userMsg]);
    this.inputMessage = '';
    this.isSending.set(true);
    this.scrollToBottom();

    const request$ =
      this.mode === 'admin'
        ? this.aiService.adminCopilot({ message: trimmed, history: historyPayload })
        : this.aiService.storefrontChat({ message: trimmed, history: historyPayload });

    request$.subscribe({
      next: (res) => {
        this.isSending.set(false);
        const assistantMsg: ChatUiMessage = {
          role: 'assistant',
          content: res.reply,
          timestamp: this.getCurrentTime(),
          recommendedProducts: res.suggestedProducts || [],
          referencedDocuments: res.referencedDocuments || []
        };
        this.messages.update((prev) => [...prev, assistantMsg]);
        this.scrollToBottom();
      },
      error: (err: { error?: { message?: string } }) => {
        this.isSending.set(false);
        const errText =
          this.mode === 'admin'
            ? err?.error?.message ||
              'ขออภัยครับ ระบบ AI ไม่สามารถเชื่อมต่อได้ในขณะนี้ กรุณาตรวจสอบ Environment Variable (GEMINI_API_KEY) หรือลองใหม่อีกครั้งครับ'
            : 'ขออภัยครับ ขณะนี้ระบบผู้ช่วย AI กำลังขัดข้องชั่วคราว กรุณาลองใหม่อีกครั้งในอีกสักครู่ครับ';
        this.messages.update((prev) => [
          ...prev,
          {
            role: 'assistant',
            content: errText,
            timestamp: this.getCurrentTime()
          }
        ]);
        this.scrollToBottom();
      }
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.messagesContainer?.nativeElement;
      if (el) {
        el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
      }
    }, 30);
  }

  addRecommendedToCart(item: SemanticProductMatchDto): void {
    const mappedProduct: ProductDto = {
      id: item.id,
      sku: item.sku,
      name: item.name,
      brand: item.brand,
      modelName: item.modelName,
      description: item.description,
      price: item.price,
      cost: item.cost,
      onHandQuantity: item.onHandQuantity,
      reservedQuantity: item.reservedQuantity,
      availableQuantity: item.availableQuantity,
      tags: item.tags || [],
      status: 'ACTIVE',
      isFeatured: false,
      soldCount: 0,
      viewCount: 0,
      specifications: item.specifications,
      categoryId: item.categoryId,
      categoryName: item.categoryName,
      image: item.imageUrl ? { url: item.imageUrl, publicId: '' } : undefined
    };
    this.cartService.addToCart(mappedProduct, 1);
  }

  getDocRoute(doc: ChatDocumentRefDto): string[] {
    if (doc.route) return [doc.route];
    if (doc.type === 'SO') return ['/admin/sales-orders', doc.id];
    if (doc.type === 'PO') return ['/admin/purchase-orders', doc.id];
    if (doc.type === 'SUPPLIER') return ['/admin/suppliers'];
    if (doc.type === 'CUSTOMER') return ['/admin/customers'];
    return ['/admin/dashboard'];
  }

  formatMessage(content: string): string {
    if (!content) return '';
    const escaped = content
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
    return escaped.replace(/\*\*(.+?)\*\*/g, '<strong class="font-bold text-white">$1</strong>');
  }

  private getCurrentTime(): string {
    const now = new Date();
    return now.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' });
  }
}
