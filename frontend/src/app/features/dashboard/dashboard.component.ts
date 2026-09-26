import { Component, inject, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  stats: any = null;
  isLoading = true;
  chart: any;

  @ViewChild('salesChart') salesChartRef!: ElementRef;

  ngOnInit() {
    this.http.get('/api/Dashboard/stats').subscribe({
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

  renderChart() {
    if (!this.salesChartRef || !this.stats?.chartData) return;

    const ctx = this.salesChartRef.nativeElement.getContext('2d');
    
    const labels = this.stats.chartData.map((d: any) => d.date);
    const dataPoints = this.stats.chartData.map((d: any) => d.amount);

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
