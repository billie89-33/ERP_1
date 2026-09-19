# Storefront Filter & Category Plan (Project Jamine)

เอกสารฉบับนี้รวบรวมลอจิกการทำงาน (Business Logic) ของระบบ Filter สินค้าจากโปรเจกต์ React เดิม เพื่อนำมาปรับใช้และพัฒนาใหม่ในโปรเจกต์ Angular (Jamine_ERP) โดยจะใช้ดีไซน์ UI รูปแบบใหม่ (Dark Theme Tailwind) ที่เข้ากับระบบปัจจุบัน

## 1. ลอจิกการทำงานจากโปรเจกต์เดิม (Legacy Features to Port)

### 1.1 Category Selection (การเลือกหมวดหมู่หลัก)
- แสดงรายการหมวดหมู่ทั้งหมด (เช่น Notebook, Monitor, Mouse, VGA)
- เมื่อผู้ใช้คลิกหมวดหมู่ ระบบจะต้องกรองสินค้าให้เหลือเฉพาะหมวดหมู่นั้นอย่างชัดเจน
- หากผู้ใช้คลิก "All" หรือ "Explore All" จะแสดงสินค้าทั้งหมด
- **API Requirement:** ส่งพารามิเตอร์ `category` ไปหา C# Backend

### 1.2 Smart Spec Filters (ระบบกรองสเปคอัจฉริยะ)
นี่คือฟีเจอร์เด่นจากโปรเจกต์เดิม ที่จะแสดง Filter เฉพาะสเปคที่เกี่ยวข้องกับหมวดหมู่สินค้านั้นๆ (ใช้ `FILTER_WHITELIST`)
- **Notebook:** CPU, RAM, Graphic Card, Display Size, Storage
- **Monitor:** Resolution, Refresh Rate, Panel Type, Display Size (in.)
- **Keyboard:** Switch Type, Connectivity, Backlight, Layout
- **Graphics Card (VGA):** Chipset, Memory Size, Interface
- **CPU:** Socket, Cores/Threads, Base Clock
- **RAM:** Type, Capacity, Speed
- **Mainboard:** Socket, Chipset, Form Factor

**กลไกการทำงาน:** 
ดึงข้อมูลจาก `Specifications (JSONB)` ของสินค้าใน Database มาดึงเฉพาะ Key ที่อยู่ใน Whitelist ด้านบน นำมาสร้างเป็น Checkbox 

### 1.3 Brand Filter (การกรองตามแบรนด์)
- ดึงรายการแบรนด์ (Brand) จากข้อมูลสินค้าที่มีในหมวดหมู่นั้นๆ 
- สามารถติ๊กเลือก (Checkbox) ได้หลายแบรนด์พร้อมกัน (เช่น MSI + LENOVO)

### 1.4 Price Range (การกรองตามช่วงราคา)
- มีช่องให้กรอกราคา Min (ต่ำสุด) และ Max (สูงสุด)

---

## 2. แผนการทำงาน (Execution Checklist)

เราจะค่อยๆ ทยอยทำทีละสเต็ป ดังนี้:

### Phase 1: เตรียม API ฝั่ง C# Backend
- [ ] 1. อัปเดต `GetProducts` API ใน `ProductsController` ให้รองรับพารามิเตอร์: `categoryId` (หรือ CategoryName), `brands` (array), `minPrice`, `maxPrice`
- [ ] 2. สร้างเส้น API ใหม่ `GET /api/Categories` เพื่อดึงรายชื่อหมวดหมู่ทั้งหมดไปแสดงเป็นปุ่มกด
- [ ] 3. สร้างเส้น API ใหม่ `GET /api/Products/filters` (หรือดึงพร้อม Category) เพื่อรวบรวมรายชื่อ Brand และค่า Spec ต่างๆ ที่มีอยู่ใน Database สำหรับสร้าง Checkbox (Dynamic Filter Options)

### Phase 2: สร้าง UI ฝั่ง Angular Frontend (โครงสร้าง Sidebar)
- [ ] 4. ออกแบบและสร้าง `SidebarComponent` (ซ้ายมือ) แยกออกมาเพื่อไม่ให้ไฟล์ Shop หลักรกเกินไป
- [ ] 5. ใช้ดีไซน์ใหม่ (Dark Theme) ตามสไตล์ของโปรเจกต์ปัจจุบัน (ยุบ/ขยาย Accordion ด้วยสไตล์เรียบหรู)
- [ ] 6. วางโครงสร้างปุ่ม Category ให้คลิกแล้วเปลี่ยน URL / อัปเดตข้อมูล

### Phase 3: การเชื่อมต่อ Logic (Smart Filter)
- [ ] 7. ใส่ลอจิก `FILTER_WHITELIST` ลงใน Angular เพื่อซ่อน/แสดงช่องกรองสเปคตาม Category ที่เลือก
- [ ] 8. เขียนฟังก์ชันให้เมื่อติ๊ก Checkbox หรือพิมพ์ราคา แล้วทำการเรียก `loadProducts()` ทันทีเพื่อดึงข้อมูลใหม่
- [ ] 9. จัดการเรื่อง Pagination ให้รีเซ็ตกลับไปหน้า 1 เสมอเมื่อมีการกด Filter ใหม่

### Phase 4: การทดสอบ (Testing)
- [ ] 10. ทดสอบคลิกหมวดหมู่ "Monitor" และเช็คว่ามี Filter "Refresh Rate" ขึ้นมาให้ติ๊ก (เช่น 100Hz, 180Hz)
- [ ] 11. ทดสอบกดติ๊กแบรนด์ MSI และเช็คว่าข้อมูลอัปเดตแบบ Real-time

### Phase 5: Angular & TypeScript Best Practices (STRICT REQUIREMENTS)
- [ ] **Strict Typing Only:** ห้ามใช้ `any` หรือ `unknown` โดยเด็ดขาด ทุกตัวแปรต้องมี Interface หรือ Type ระบุชัดเจน (เช่น `ProductDto`, `CategoryDto`, `FilterState`)
- [ ] **Reactive Programming:** ใช้ Signals (หรือ RxJS) สำหรับจัดการ State ของ Filter เพื่อให้การอัปเดต UI ลื่นไหลและเป็น Best Practice ของ Angular 18
- [ ] **Standalone Components:** สร้าง Component ใหม่ด้วย Standalone API เสมอ ไม่พึ่งพา NgModule
- [ ] **Bug Checking:** ตรวจสอบ Console Error, Network Request และความถูกต้องของ Data Binding ในทุกๆ ขั้นตอนของการพัฒนา
- [ ] **Full Integration & Routing:** ทุกลิงก์ในหน้าเว็บต้องสามารถกดข้ามไปมาได้จริง (เช่น จาก Sidebar ไปหน้ารายละเอียดสินค้า) และต้องต่อ API เชื่อมกับ Backend ให้เสร็จสมบูรณ์ 100% ห้ามทิ้งเป็นข้อมูลจำลอง (Mock data) เด็ดขาด

---

## 3. การปรับปรุง UI/UX และ Mobile Design (จากของเดิม)

เพื่อให้ระบบใช้งานง่ายขึ้น (User-friendly) และรองรับมือถือ เราจะปรับปรุงจากโปรเจกต์ React เดิมดังนี้:

### 3.1 ความเรียบง่าย (Simplicity & Modern Dark Theme)
- **ลดทอนสีสันที่กวนสายตา:** เปลี่ยนจากพื้นหลังเบลอสีม่วง/น้ำเงินที่ดูหนาแน่น มาใช้ **Dark Theme (สีเทา-ดำ Slate)** สลับตัดกับสีหลัก (Accent Color) สีฟ้าหรือสีคราม เพื่อให้อุปกรณ์คอมพิวเตอร์โดดเด่นขึ้น
- **Checkbox ที่กดง่ายขึ้น:** ขยายขนาดพื้นที่กด (Hitbox) ของปุ่ม Filter ให้ใหญ่ขึ้น ลดความซับซ้อนของ Animation ลงเพื่อให้เว็บโหลดเร็วและดู Professional

### 3.2 Mobile-First Design (การรองรับมือถือ)
- **ซ่อน Sidebar บนมือถือ:** หน้าจอเล็ก (โทรศัพท์) Sidebar จะไม่เกะกะพื้นที่ แต่จะถูกเก็บรวบเป็นปุ่ม 🎛️ **"Filter / กรองสินค้า"** ลอยอยู่ด้านล่าง หรือติดอยู่ใต้ช่อง Search
- **Off-Canvas Drawer (แผงสไลด์ด้านข้าง):** เมื่อผู้ใช้กดปุ่ม Filter บนมือถือ แผงหมวดหมู่และสเปคจะสไลด์ออกมาจากด้านซ้ายหรือขวา (เต็มจอ) ทำให้กดเลือกได้ง่ายดายเหมือนแอปพลิเคชัน (App-like experience)
- **Sticky Add to Cart:** ในหน้ารายละเอียดสินค้า ปุ่ม "ใส่ตะกร้า" บนมือถือจะลอยติดอยู่ขอบจอด้านล่างเสมอ เพื่อให้ลูกค้ากดซื้อได้ทันทีโดยไม่ต้องเลื่อนหา

---

## 4. โครงสร้างข้อมูลที่แนะนำ (Recommended Data Structures)

เพื่อให้ระบบ Filter ทำงานได้อย่างฉลาดแบบ "Dynamic Data-Driven" (ไม่สคริปต์ตายตัว) ขอแนะนำให้จัดรูป Data Structure ดังนี้:

### 4.1 Backend Response (C# DTO)
เมื่อ Frontend เรียก API ขอดูตัวกรองของหมวดหมู่นั้นๆ (เช่น `GET /api/Products/filters?categoryId=...`) Backend จะคำนวณและส่งโครงสร้างนี้กลับมา:

```csharp
public class FilterOptionsDto 
{
    // รายชื่อแบรนด์ทั้งหมดที่มีในหมวดหมู่นี้
    public List<string> Brands { get; set; } = new();

    // รายชื่อสเปค และค่าที่มีให้เลือก (คำนวณจาก JSONB Specifications)
    // เช่น { "Refresh Rate": ["100Hz", "180Hz"], "Panel Type": ["VA", "IPS"] }
    public Dictionary<string, List<string>> AvailableSpecs { get; set; } = new();

    // ราคาสูงสุดและต่ำสุดในหมวดหมู่นี้ เพื่อเอาไปทำสไลเดอร์ช่วงราคา
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
}
```
**ข้อดี:** Frontend ไม่ต้อง Hardcode `FILTER_WHITELIST` อีกต่อไป! ถ้าวันนึงเราเพิ่มสินค้าแปลกๆ เข้ามา Backend จะกวาด JSONB และส่งตัวกรองใหม่มาให้ Frontend แสดงผลอัตโนมัติ

### 4.2 Frontend State Management (Angular TypeScript)
สร้าง Interface สำหรับเก็บ State ของผู้ใช้ที่กดเลือก Filter ไว้ (ใช้งานคู่กับ Angular Signals)

```typescript
export interface FilterState {
  categoryId: string | null;      // หมวดหมู่ที่เลือก (null = All)
  searchQuery: string;            // คำค้นหา
  brands: string[];               // แบรนด์ที่ติ๊กเลือก (เช่น ['MSI', 'SAMSUNG'])
  minPrice: number | null;
  maxPrice: number | null;
  
  // สเปคที่ติ๊กเลือก ตัวอย่าง: { 'Refresh Rate': ['180Hz'], 'Resolution': ['FHD'] }
  selectedSpecs: Record<string, string[]>; 
  
  // Pagination
  page: number;
  limit: number;
}
```
**ข้อดี:** เมื่อใช้ Signals ควบคู่กับ Interface นี้ (เช่น `filterState = signal<FilterState>(...)`) แค่ข้อมูลใน Signal เปลี่ยน Angular จะยิง API และวาด UI ใหม่ทันที โค้ดจะสะอาดมาก ไม่มี `any` หลุดมาเลย
