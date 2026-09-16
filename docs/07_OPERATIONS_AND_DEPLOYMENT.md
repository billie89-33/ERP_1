# Operations, DevOps & Deployment Strategy

เพื่อให้โปรเจกต์ ERP นี้นำไปใช้งานได้จริง (Production-Ready) และมีมาตรฐานระดับ Enterprise เราได้กำหนดแผนการจัดการส่วนเสริมและระบบคลาวด์ (Cloud Infrastructure) ไว้ 5 ส่วนดังนี้:

## 1. การจัดการไฟล์และรูปภาพ (Media Storage) 🖼️
**เครื่องมือที่เลือกใช้: Cloudinary**
*   **ทำไมถึงใช้:** ระบบ Database (Supabase/PostgreSQL) ไม่เหมาะกับการเก็บไฟล์รูปภาพขนาดใหญ่โดยตรง
*   **การทำงาน:** เมื่อมีการอัปโหลดรูปสินค้า (หรือแนบไฟล์ PDF บิลต่างๆ) จากฝั่ง Angular:
    1. C# API จะรับไฟล์และส่งต่อ (Upload) ขึ้นไปเก็บที่เซิร์ฟเวอร์ของ **Cloudinary**
    2. Cloudinary จะส่ง URL ของรูปภาพกลับมา (เช่น `https://res.cloudinary.com/.../image.jpg`)
    3. C# จะนำ URL นี้ไปบันทึกลงในคอลัมน์ `ImageUrl` ใน Database

## 2. ระบบจัดการข้อผิดพลาดแบบผสมผสาน (Hybrid Error Handling & Try/Catch) 🛡️
**เครื่องมือที่เลือกใช้: C# Exception Middleware + Strategic `try/catch`**
*   **การดักจับภาพรวม (Global):** เราใช้ Middleware กั้นไว้ตรงกลาง เพื่อดักจับ Error ที่หลุดรอดหรือไม่ได้คาดคิด (Unhandled Exceptions) ป้องกันไม่ให้แฮกเกอร์เห็น Stack Trace โดยแปลงเป็น JSON มาตรฐาน เช่น `{ "statusCode": 500, "message": "ระบบขัดข้อง" }`
*   **การใช้ `try/catch` แบบครอบคลุม (Strategic):** แม้จะมี Middleware แต่เรา **ยังคงบังคับใช้ `try/catch` อย่างครอบคลุม** ในจุดที่มีความเสี่ยงสูงทางธุรกิจ (Business Critical) เพื่อให้ระบบฟื้นตัวได้ (Recovery) เช่น:
    *   จุดที่มีการอัปโหลดไฟล์ไป Cloudinary (ถ้าอัปโหลดรูปไม่ผ่าน ให้ catch เพื่อเซฟข้อมูลลง DB ก่อนโดยไม่มีรูป)
    *   จุดที่มีการบันทึกข้อมูลหลายตารางพร้อมกัน (ใช้ร่วมกับ Transaction เพื่อดัก catch แล้วสั่ง Rollback)
    *   จุดที่รับพารามิเตอร์แปลกๆ หรือเรียก API ภายนอก

## 3. การตรวจสอบข้อมูล (Data Validation & DTO) 🚦
**เครื่องมือที่เลือกใช้: FluentValidation (C#)**
*   **การทำงาน:** ก่อนที่ C# จะยอมนำข้อมูลไปบันทึกลง Database ต้องผ่านการตรวจสอบ (Validate) อย่างเข้มงวด
*   เราจะสร้าง Data Transfer Object (DTO) มารับข้อมูลจาก Angular และใช้ FluentValidation ตั้งกฎ เช่น:
    *   `RuleFor(x => x.Price).GreaterThan(0)` (ราคาห้ามติดลบ)
    *   `RuleFor(x => x.Email).EmailAddress()` (ต้องเป็นรูปแบบอีเมลเท่านั้น)

## 4. คู่มือ API อัตโนมัติ (API Documentation) 📖
**เครื่องมือที่เลือกใช้: Built-in Swagger (.NET Core)**
*   **การทำงาน:** ไม่ต้องเขียนคู่มือเอกสารเอง ระบบจะสร้างหน้าเว็บคู่มือ (Swagger UI) ให้อัตโนมัติจากการสแกนโค้ด C# Controller 
*   **ประโยชน์:** ฝั่งคนพัฒนา Angular สามารถเปิดหน้า Swagger เพื่อดูว่ามี API URL อะไรให้ยิงบ้าง และสามารถทดลองกรอกข้อมูลจำลองเพื่อกดส่ง (Execute) เทส API ได้ทันที

---

## 🚀 5. สถาปัตยกรรมการนำขึ้นระบบจริง (Deployment Architecture)

เมื่อเขียนโค้ดเสร็จ เราจะแยกนำโปรเจกต์ขึ้น Cloud (ฟรีสำหรับ Tier เริ่มต้น) ดังนี้:

### 🌐 Frontend (Angular) 👉 นำขึ้น Vercel
*   Angular จะถูก Build เป็นไฟล์ Static (HTML, CSS, JS) และนำไปฝากไว้ที่ **Vercel**
*   Vercel จะโหลดหน้าเว็บให้ผู้ใช้ได้เร็วมาก (ผ่าน CDN) และอัปเดตเว็บให้อัตโนมัติเวลาเรา Push โค้ดขึ้น GitHub

### ⚙️ Backend API (C# .NET Core) 👉 นำขึ้น Render.com
*   C# Web API จะนำไปรันบน **Render (Web Service)** 
*   ทำหน้าที่เป็นสมองของระบบ คอยประมวลผล Business Logic และเป็นสะพานเชื่อมระหว่าง Angular กับ Database

### 🗄️ Database (PostgreSQL) 👉 นำขึ้น Supabase
*   ฐานข้อมูลหลักทั้งหมด (รวมถึง JSONB Specifications) จะถูกโฮสต์อยู่บน **Supabase**
*   C# (Render) จะคุยกับ Database (Supabase) ผ่าน Connection String ที่ตั้งค่าไว้

> [!NOTE] 
> **สรุปการไหลของข้อมูล (Data Flow):**
> ผู้ใช้เปิดเว็บที่โฮสต์บน **[Vercel]** ➡️ กดบันทึกข้อมูล ➡️ ข้อมูลวิ่งไปหา C# API ที่ **[Render.com]** ➡️ C# ตรวจสอบความถูกต้องและบันทึกลงฐานข้อมูล **[Supabase]** (ถ้ารูปภาพ จะส่งไป **[Cloudinary]**)
