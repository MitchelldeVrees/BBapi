import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

type WeatherForecast = {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
};

@Component({
  selector: 'app-root',
  imports: [NgIf, NgFor, DatePipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'BBapi Weather Board';
  forecasts: WeatherForecast[] = [];
  isLoading = false;
  errorMessage = '';

  private readonly apiUrl = 'http://localhost:5088/weatherforecast';

  constructor(private readonly http: HttpClient) {}

  ngOnInit(): void {
    this.loadForecast();
  }

  loadForecast(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.http.get<WeatherForecast[]>(this.apiUrl).subscribe({
      next: (data) => {
        this.forecasts = data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Could not reach the backend API. Make sure the .NET server is running on http://localhost:5088.';
        this.isLoading = false;
      }
    });
  }
}
