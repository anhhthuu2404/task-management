import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CommentAttachment {
  fileName: string;
  fileContent?: string;
  fileUrl?: string;
}

export interface CommentInput {
  text: string;
  attachments?: CommentAttachment[];
}

export interface SubTaskInput {
  title: string;
  assigneeId?: string;
}

export interface ChecklistInput {
  title: string;
}

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:44399/api/app/task';

  // --- Task Detail & Main Actions ---
  getTaskDetail(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}/detail`);
  }

  updateStatus(id: string, status: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/status?status=${status}`, {});
  }

  // --- Comment Actions ---
  getComments(taskId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${taskId}/comments`);
  }

  createComment(taskId: string, input: CommentInput): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${taskId}/comment`, input);
  }

  updateComment(commentId: string, text: string): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/comment/${commentId}`, { text });
  }

  deleteComment(commentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/comment/${commentId}`);
  }

  // --- SubTask Actions ---
  createSubTask(taskId: string, input: SubTaskInput): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${taskId}/sub-task`, input);
  }

  updateSubTask(subTaskId: string, input: SubTaskInput): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/sub-task/${subTaskId}`, input);
  }

  toggleSubTask(subTaskId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/sub-task/${subTaskId}/toggle`, {});
  }

  deleteSubTask(subTaskId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/sub-task/${subTaskId}`);
  }

  // --- Checklist Actions ---
  createChecklist(taskId: string, input: ChecklistInput): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${taskId}/checklist-item`, input);
  }

  updateChecklist(itemId: string, input: ChecklistInput): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/checklist-item/${itemId}`, input);
  }

  toggleChecklist(itemId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/checklist-item/${itemId}/toggle`, {});
  }

  deleteChecklist(itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/checklist-item/${itemId}`);
  }
}