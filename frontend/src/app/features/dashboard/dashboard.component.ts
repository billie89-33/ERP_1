import { Component, inject, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { DashboardStatsDto, DashboardChartPointDto } from '../../core/models/erp.model';
import { AiService } from '../../core/services/ai.service';
import { DashboardAiBriefDto } from '../../core/models/ai.model';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private aiService = inject(AiService);

  stats: DashboardStatsDto | null = null;
  isLoading = true;
  chart: Chart | null = null;

  aiBrief: DashboardAiBriefDto | null = null;
  isGeneratingBrief = false;
  briefError = '';

  @ViewChild('salesChart') salesChartRef!: ElementRef<HTMLCanvasElement>;

  ngOnInit() {
    this.http.get<DashboardStatsDto>('/api/Dashboard/stats').subscribe({
      next: (data) => {
        this.stats = data;
        this.isLoading = false;
        
        // Wait for next tick so canvas is rendered
        setTimeout(() => this.renderChart(), 0);
      },
      error: (err) => {
        console.error('Failed to load stats', err);
        this.isLoading = false;
      }
    });
  }

  generateAiBrief() {
    if (this.isGeneratingBrief) return;
    this.isGeneratingBrief = true;
    this.briefError = '';

    this.aiService.generateDashboardBrief().subscribe({
      next: (res) => {
        this.isGeneratingBrief = false;
        this.aiBrief = res;
      },
      error: (err: { error?: { message?: string } }) => {
        this.isGeneratingBrief = false;
        this.briefError = err?.error?.message || 'ไม่สามารถสร้างบทวิเคราะห์ AI ได้ในขณะนี้ กรุณาตรวจสอบ GEMINI_API_KEY';
      }
    });
  }

  renderChart() {
    if (!this.salesChartRef || !this.stats?.chartData) return;

    const ctx = this.salesChartRef.nativeElement.getContext('2d');
    if (!ctx) return;
    
    const labels = this.stats.chartData.map((d: DashboardChartPointDto) => d.date);
    const dataPoints = this.stats.chartData.map((d: DashboardChartPointDto) => d.amount);

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: 'Sales Revenue (THB)',
          data: dataPoints,
          borderColor: '#10b981', // emerald-500
          backgroundColor: 'rgba(16, 185, 129, 0.1)',
          borderWidth: 3,
          pointBackgroundColor: '#10b981',
          pointBorderColor: '#1e293b', // slate-800
          pointBorderWidth: 2,
          pointRadius: 4,
          fill: true,
          tension: 0.4 // Smooth curves
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#1e293b',
            titleColor: '#f8fafc',
            bodyColor: '#f8fafc',
            borderColor: '#334155',
            borderWidth: 1,
            padding: 10
          }
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { color: '#64748b' } // slate-500
          },
          y: {
            grid: { color: '#334155' }, // slate-700
            ticks: {
              color: '#64748b',
              callback: function(value) {
                return '฿' + value;
              }
            }
          }
        }
      }
    });
  }
}
