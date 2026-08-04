import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';

@Component({
  selector: 'app-chart-card',
  standalone: true,
  imports: [BaseChartDirective],
  templateUrl: './chart-card.component.html',
  styleUrls: ['./chart-card.component.scss'],
})
export class ChartCardComponent {
  readonly title = input('');
  readonly dataPoints = input<{ weekLabel: string; value: number }[]>([]);
  readonly chartType = input<'line' | 'bar'>('line');

  readonly chartData = computed(() => {
    const points = this.dataPoints();
    const labels = points.map((point) => point.weekLabel);
    const values = points.map((point) => point.value);

    return {
      labels,
      datasets: [
        {
          data: values,
          borderColor: '#0F6E56',
          backgroundColor: '#0F6E56',
          tension: 0.35,
          maxBarThickness: 40,
          barPercentage: 0.5,
        },
      ],
    };
  });

  readonly chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
      tooltip: {
        enabled: true,
      },
    },
    scales: {
      x: {
        display: true,
        grid: {
          display: false,
        },
        ticks: {
          font: {
            size: 11,
          },
        },
      },
      y: {
        display: true,
        beginAtZero: true,
        grid: {
          color: '#EEEEEE',
        },
        ticks: {
          font: {
            size: 11,
          },
        },
      },
    },
  };
}
