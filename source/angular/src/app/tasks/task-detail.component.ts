import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ConfirmationService, Confirmation, ToasterService } from '@abp/ng.theme.shared';
import { TaskService, CommentAttachment, CommentInput } from './task.service';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './task-detail.component.html'
})
export class TaskDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly taskService = inject(TaskService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);

  readonly backendUrl = 'https://localhost:44399';

  taskId = '';
  taskDetail: any = null;
  isLoading = true;
  activeTab: 'comments' | 'subtask' | 'checklist' | 'timeline' = 'comments';

  // State Comment & Files
  comments: any[] = [];
  newCommentText = '';
  selectedFiles: CommentAttachment[] = [];
  isUploading = false;

  // Edit Comment State
  editingCommentId: string | null = null;
  editingCommentText = '';

  // SubTask State
  newSubTaskTitle = '';
  editingSubTaskId: string | null = null;
  editingSubTaskTitle = '';

  // Checklist State
  newChecklistTitle = '';
  editingChecklistItemId: string | null = null;
  editingChecklistTitle = '';

  ngOnInit(): void {
    this.taskId = this.route.snapshot.params['id'];
    if (this.taskId) {
      this.loadTaskDetail();
    }
  }

  loadTaskDetail(): void {
    this.isLoading = true;
    this.taskService.getTaskDetail(this.taskId).subscribe({
      next: (res) => {
        this.taskDetail = res;
        this.loadComments();
        this.isLoading = false;
      },
      error: () => {
        this.toaster.error('Không thể tải thông tin công việc.');
        this.isLoading = false;
      }
    });
  }

  changeStatus(status: number): void {
    this.taskService.updateStatus(this.taskId, status).subscribe({
      next: () => {
        this.taskDetail.status = status;
        this.toaster.success('Cập nhật trạng thái công việc thành công.');
      },
      error: () => this.toaster.error('Không thể cập nhật trạng thái.')
    });
  }

  // --- Comment Logic ---
  loadComments(): void {
    this.taskService.getComments(this.taskId).subscribe({
      next: (res) => {
        this.comments = res || [];
      },
      error: () => this.toaster.error('Không thể tải danh sách bình luận.')
    });
  }

  onFilesSelected(event: any): void {
    const files: FileList = event.target.files;
    if (!files || files.length === 0) return;

    Array.from(files).forEach((file) => {
      if (file.size > 10 * 1024 * 1024) {
        this.toaster.warn(`Tệp ${file.name} vượt quá 10MB.`);
        return;
      }

      const reader = new FileReader();
      reader.onload = () => {
        this.selectedFiles.push({
          fileName: file.name,
          fileContent: reader.result as string
        });
      };
      reader.readAsDataURL(file);
    });

    event.target.value = '';
  }

  removeSelectedFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  addComment(): void {
    if (!this.newCommentText.trim() && this.selectedFiles.length === 0) return;

    this.isUploading = true;
    const payload: CommentInput = {
      text: this.newCommentText,
      attachments: this.selectedFiles
    };

    this.taskService.createComment(this.taskId, payload).subscribe({
      next: () => {
        this.newCommentText = '';
        this.selectedFiles = [];
        this.isUploading = false;
        this.toaster.success('Đã gửi bình luận!');
        this.loadComments();
      },
      error: () => {
        this.isUploading = false;
        this.toaster.error('Không thể gửi bình luận.');
      }
    });
  }

  startEditComment(comment: any): void {
    this.editingCommentId = comment.id;
    this.editingCommentText = comment.text;
  }

  cancelEditComment(): void {
    this.editingCommentId = null;
    this.editingCommentText = '';
  }

  saveComment(comment: any): void {
    if (!this.editingCommentText.trim()) return;

    this.taskService.updateComment(comment.id, this.editingCommentText).subscribe({
      next: (res) => {
        comment.text = res.text || this.editingCommentText;
        this.editingCommentId = null;
        this.toaster.success('Đã cập nhật bình luận!');
      },
      error: () => this.toaster.error('Không thể cập nhật bình luận.')
    });
  }

  deleteComment(commentId: string): void {
    this.confirmation
      .warn('Bạn có chắc chắn muốn xóa bình luận này?', 'Xác nhận xóa')
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm) {
          this.taskService.deleteComment(commentId).subscribe(() => {
            this.toaster.success('Đã xóa bình luận.');
            this.loadComments();
          });
        }
      });
  }

  // --- SubTask Logic ---
  addSubTask(): void {
    if (!this.newSubTaskTitle.trim()) return;
    this.taskService.createSubTask(this.taskId, { title: this.newSubTaskTitle }).subscribe(() => {
      this.newSubTaskTitle = '';
      this.toaster.success('Thêm công việc phụ thành công!');
      this.loadTaskDetail();
    });
  }

  startEditSubTask(st: any): void {
    this.editingSubTaskId = st.id;
    this.editingSubTaskTitle = st.title;
  }

  cancelEditSubTask(): void {
    this.editingSubTaskId = null;
    this.editingSubTaskTitle = '';
  }

  saveSubTask(st: any): void {
    if (!this.editingSubTaskTitle.trim()) return;
    this.taskService.updateSubTask(st.id, { title: this.editingSubTaskTitle, assigneeId: st.assigneeId }).subscribe((res) => {
      st.title = res.title;
      this.editingSubTaskId = null;
      this.toaster.success('Đã cập nhật công việc phụ!');
    });
  }

  toggleSubTask(st: any): void {
    this.taskService.toggleSubTask(st.id).subscribe(() => st.isCompleted = !st.isCompleted);
  }

  deleteSubTask(stId: string): void {
    this.confirmation.warn('Xóa công việc phụ này?', 'Xác nhận').subscribe((status) => {
      if (status === Confirmation.Status.confirm) {
        this.taskService.deleteSubTask(stId).subscribe(() => this.loadTaskDetail());
      }
    });
  }

  // --- Checklist Logic ---
  addChecklistItem(): void {
    if (!this.newChecklistTitle.trim()) return;
    this.taskService.createChecklist(this.taskId, { title: this.newChecklistTitle }).subscribe(() => {
      this.newChecklistTitle = '';
      this.loadTaskDetail();
    });
  }

  startEditChecklist(item: any): void {
    this.editingChecklistItemId = item.id;
    this.editingChecklistTitle = item.title;
  }

  cancelEditChecklist(): void {
    this.editingChecklistItemId = null;
    this.editingChecklistTitle = '';
  }

  saveChecklist(item: any): void {
    if (!this.editingChecklistTitle.trim()) return;
    this.taskService.updateChecklist(item.id, { title: this.editingChecklistTitle }).subscribe((res) => {
      item.title = res.title;
      this.editingChecklistItemId = null;
    });
  }

  toggleChecklistItem(item: any): void {
    this.taskService.toggleChecklist(item.id).subscribe(() => item.isDone = !item.isDone);
  }

  deleteChecklistItem(itemId: string): void {
    this.confirmation.warn('Xóa mục kiểm tra này?', 'Xác nhận').subscribe((status) => {
      if (status === Confirmation.Status.confirm) {
        this.taskService.deleteChecklist(itemId).subscribe(() => this.loadTaskDetail());
      }
    });
  }

  // --- URL & Helpers ---
  openFile(filePath: string): void {
    if (!filePath) return;
    let url = filePath;
    if (!filePath.startsWith('http://') && !filePath.startsWith('https://')) {
      const base = this.backendUrl.replace(/\/+$/, '');
      const path = filePath.startsWith('/') ? filePath : '/' + filePath;
      url = `${base}${path}`;
    }
    window.open(url, '_blank');
  }

  getFileIcon(filename: string): string {
    const ext = filename?.split('.').pop()?.toLowerCase();
    switch (ext) {
      case 'pdf': return 'bi-file-earmark-pdf text-danger';
      case 'doc': case 'docx': return 'bi-file-earmark-word text-primary';
      case 'xls': case 'xlsx': return 'bi-file-earmark-excel text-success';
      case 'zip': case 'rar': return 'bi-file-earmark-zip text-warning';
      case 'png': case 'jpg': case 'jpeg': return 'bi-file-earmark-image text-info';
      default: return 'bi-file-earmark text-secondary';
    }
  }

  getChecklistProgress(): number {
    if (!this.taskDetail?.checklistItems?.length) return 0;
    const done = this.taskDetail.checklistItems.filter((x: any) => x.isDone).length;
    return Math.round((done / this.taskDetail.checklistItems.length) * 100);
  }

  getPriorityBadge(priority: number): { text: string; cssClass: string } {
    const maps: Record<number, { text: string; cssClass: string }> = {
      0: { text: 'Thấp', cssClass: 'bg-secondary-subtle text-secondary border' },
      1: { text: 'Trung bình', cssClass: 'bg-info-subtle text-info-emphasis border' },
      2: { text: 'Cao', cssClass: 'bg-warning-subtle text-warning-emphasis border' },
      3: { text: 'Khẩn cấp', cssClass: 'bg-danger-subtle text-danger border' }
    };
    return maps[priority] || { text: 'N/A', cssClass: 'bg-light text-dark' };
  }

  getStatusBadge(status: number): { text: string; cssClass: string } {
    const maps: Record<number, { text: string; cssClass: string }> = {
      0: { text: 'Mới', cssClass: 'bg-secondary-subtle text-secondary border' },
      1: { text: 'Đang làm', cssClass: 'bg-primary-subtle text-primary border' },
      2: { text: 'Hoàn thành', cssClass: 'bg-success-subtle text-success border' },
      3: { text: 'Đã hủy', cssClass: 'bg-danger-subtle text-danger border' }
    };
    return maps[status] || { text: 'N/A', cssClass: 'bg-light text-dark' };
  }
}