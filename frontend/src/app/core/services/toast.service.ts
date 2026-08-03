import { Injectable, signal } from '@angular/core';

interface Toast {
  id: number;
  message: string;
  type: 'error' | 'success';
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private readonly toasts = signal<Toast[]>([]);
  readonly toastsView = this.toasts.asReadonly();

  private nextId = 0;

  show(message: string, type: 'error' | 'success' = 'error'): void {
    const id = this.nextId++;
    this.toasts.set([...this.toasts(), { id, message, type }]);

    setTimeout(() => this.remove(id), 5000);
  }

  remove(id: number): void {
    this.toasts.set(this.toasts().filter((toast) => toast.id !== id));
  }
}
