# Backend Architecture (C# .NET Core)
**Framework:** .NET 8 (LTS) - Industry Standard for Stability
**Project Type:** ASP.NET Core Web API
**ORM (Object-Relational Mapper):** Entity Framework Core (EF Core)

## 🏗️ โครงสร้างการทำงาน (Architecture Flow)
ตัว Backend จะทำหน้าที่เป็นศูนย์กลาง Business Logic และปกป้องความถูกต้องของข้อมูล (Source of Truth)

### 1. Controllers / Minimal APIs (Endpoints)
- **หน้าที่:** เป็นประตูรับ Request (JSON) ที่ยิงมาจากฝั่ง Angular
- **ตัวอย่าง:** `[HttpGet("api/products")]`

### 2. Services / Business Logic Layer
- **หน้าที่:** ทำการประมวลผล เช่น เช็คว่าสต๊อกพอตัดหรือไม่, ตรวจสอบวงเงินเครดิตลูกค้า (B2B)

### 3. Entity Framework Core (Database Access)
- **หน้าที่:** แปลง C# Class (Model) ให้กลายเป็นคำสั่ง SQL อัตโนมัติ (Code-First Migration)
- **คุณสมบัติเด่น:** รองรับคอลัมน์แบบ JSONB สำหรับข้อมูลที่ยืดหยุ่น (เช่น Specifications)

---

## 🛡️ แนวทางการออกแบบเพื่ออนาคต (Architecture Patterns)

### 1. Soft Delete Pattern (ห้ามใช้ DELETE)
เพื่อป้องกันไม่ให้ประวัติข้อมูลบัญชีเสียหาย (Data Loss) จากปัญหา Foreign Key (FK) Constraint
- ทุก Model จะมีฟิลด์ `public bool IsDeleted { get; set; } = false;`
- เวลาลบข้อมูล จะใช้คำสั่ง Update `IsDeleted = true` แทนการสั่ง Delete ทิ้งจาก Database

### 2. Loose Coupling (Logical FK) - ใช้สำหรับระบบ Microservices
หากในอนาคตต้องการแยกระบบ หรือมีข้อมูลที่ดิ้นได้สูง
- เราจะเก็บแค่ `ReferenceId` (Guid) ไว้ในคอลัมน์
- เราจะไม่สร้างการเชื่อมโยง Foreign Key (Physical FK) ระดับ Database
- **การควบคุม:** C# Business Logic จะเป็นคนตรวจสอบความถูกต้องของ `ReferenceId` ด้วยตัวเองแทน Database
