import { Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ToasterService } from '@abp/ng.theme.shared';
import { SessionStateService } from '@abp/ng.core';

import { 
  TaskService, 
  TaskStatus, 
  TaskPriority, 
  TaskDetailDto, 
  TaskFileDto 
} from './task.service';

export interface LocalTaskDetailDto extends TaskDetailDto {
  fileUrl?: string;
  fileName?: string;
  submissionFiles?: TaskFileDto[] | any[];
  submissionFileName?: string;
  submissionFileUrl?: string;
  attachments?: any[];
  subTasks?: any[];
  checklistItems?: any[];
  activityLogs?: any[];
}

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule]
})
export class TaskDetailComponent implements OnInit {
  @ViewChild('submitFileInput') submitFileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  taskId: string = '';
  taskDetail: LocalTaskDetailDto | null = null;
  isLoading: boolean = true;
  isActionLoading: boolean = false;
  currentUserId: string | null = null;

  // Tab State
  activeTab: 'comments' | 'subtask' | 'checklist' | 'timeline' = 'comments';

  // Comment State
  comments: any[] = [];
  newCommentText: string = '';
  selectedFiles: { fileName: string; fileContent: string; rawFile?: File }[] = [];
  isUploading: boolean = false;
  editingCommentId: string | null = null;
  editingCommentText: string = '';

  // SubTask State
  newSubTaskTitle: string = '';
  editingSubTaskId: string | null = null;
  editingSubTaskTitle: string = '';

  // Checklist State
  newChecklistTitle: string = '';
  editingChecklistItemId: string | null = null;
  editingChecklistTitle: string = '';

  // State báo cáo tạm thời
  localSubmittedNote: string = '';
  localSubmittedFiles: any[] = [];
  isSubmittedLocally: boolean = false;

  // Modal Nộp Báo Cáo
  isSubmitModalOpen: boolean = false;
  submissionNote: string = '';
  submissionFiles: File[] = [];

  // Modal Từ Chối
  isRejectModalOpen: boolean = false;
  rejectReason: string = '';

  readonly TaskStatus = TaskStatus;
  readonly TaskPriority = TaskPriority;

  constructor(
    private route: ActivatedRoute,
    private taskService: TaskService,
    private toaster: ToasterService,
    private cdr: ChangeDetectorRef,
    private location: Location,
    private sanitizer: DomSanitizer,
    private sessionState: SessionStateService
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.sessionState.getCurrentUser()?.id || null;
    this.taskId = this.route.snapshot.paramMap.get('id') || '';
    if (this.taskId) {
      this.loadTaskDetail();
    } else {
      this.isLoading = false;
      this.toaster.error('Không tìm thấy mã công việc!', 'Lỗi');
    }
  }

  goBack(): void {
    this.location.back();
  }

  get activityLogsList(): any[] {
    if (!this.taskDetail) return [];
    const raw = this.taskDetail as any;
    return raw.activityLogs || raw.ActivityLogs || raw.logs || raw.activities || raw.histories || raw.auditLogs || [];
  }

  loadTaskDetail(isSilent: boolean = false): void {
    if (!isSilent) this.isLoading = true;
    this.taskService.getTaskDetail(this.taskId).subscribe({
      next: (data: TaskDetailDto) => {
        this.taskDetail = data as LocalTaskDetailDto;
        const raw = data as any;

        // ✅ Đồng bộ mảng bình luận chuẩn trực tiếp từ Server
        this.comments = raw.comments 
          || raw.Comments 
          || raw.commentDtos 
          || raw.CommentDtos 
          || raw.taskComments 
          || raw.TaskComments
          || raw.comments?.items 
          || raw.Comments?.items 
          || [];

        if (!this.taskDetail.subTasks) {
          this.taskDetail.subTasks = raw.subTaskDtos || raw.SubTaskDtos || raw.subTasks || raw.SubTasks || [];
        }
        if (!this.taskDetail.checklistItems) {
          this.taskDetail.checklistItems = raw.checklistItemDtos || raw.ChecklistItemDtos || raw.checklistItems || raw.ChecklistItems || [];
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.isLoading = false;
        this.toaster.error(err.error?.error?.message || 'Không thể tải thông tin công việc!', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- BÌNH LUẬN ---
  async onFilesSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      const filesArr = Array.from(input.files);
      for (const file of filesArr) {
        const base64 = await this.fileToBase64(file);
        this.selectedFiles.push({
          fileName: file.name,
          fileContent: base64,
          rawFile: file
        });
      }
      input.value = '';
      this.cdr.detectChanges();
    }
  }

  removeSelectedFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  addComment(): void {
    const text = this.newCommentText.trim();
    if (!text && this.selectedFiles.length === 0) return;

    this.isUploading = true;
    const payload = {
      taskId: this.taskId,
      text: text,
      attachments: this.selectedFiles.map(f => ({
        fileName: f.fileName,
        fileContent: f.fileContent
      }))
    };

    this.taskService.createComment(payload).subscribe({
      next: () => {
        this.isUploading = false;
        this.newCommentText = '';
        this.selectedFiles = [];
        this.toaster.success('Thêm bình luận thành công.', 'Thông báo');
        
        // Reload trực tiếp từ Database
        this.loadTaskDetail(true);
      },
      error: (err: any) => {
        this.isUploading = false;
        this.toaster.error(err.error?.error?.message || 'Không thể gửi bình luận.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  startEditComment(comment: any): void {
    this.editingCommentId = comment.id;
    this.editingCommentText = comment.text || '';
  }

  cancelEditComment(): void {
    this.editingCommentId = null;
    this.editingCommentText = '';
  }

  saveComment(comment: any): void {
    if (!this.editingCommentText.trim()) return;

    if (String(comment.id).startsWith('temp-') || /^\d+$/.test(String(comment.id))) {
      comment.text = this.editingCommentText;
      this.cancelEditComment();
      this.toaster.success('Đã cập nhật bình luận.', 'Thông báo');
      this.cdr.detectChanges();
      return;
    }

    this.taskService.updateComment(comment.id, { text: this.editingCommentText }).subscribe({
      next: () => {
        comment.text = this.editingCommentText;
        this.cancelEditComment();
        this.toaster.success('Đã cập nhật bình luận.', 'Thông báo');
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật bình luận.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  deleteComment(commentId: string): void {
    if (!confirm('Bạn có chắc chắn muốn xóa bình luận này?')) return;

    if (String(commentId).startsWith('temp-') || /^\d+$/.test(String(commentId))) {
      this.comments = this.comments.filter(c => c.id !== commentId);
      this.toaster.success('Đã xóa bình luận.', 'Thông báo');
      this.cdr.detectChanges();
      return;
    }

    this.taskService.deleteComment(commentId).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.id !== commentId);
        this.toaster.success('Đã xóa bình luận.', 'Thông báo');
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.toaster.error(err.error?.error?.message || 'Không thể xóa bình luận.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- SUBTASKS ---
  addSubTask(): void {
    const title = this.newSubTaskTitle.trim();
    if (!title || !this.taskDetail) return;
    if (!this.taskDetail.subTasks) this.taskDetail.subTasks = [];

    const titleToSend = title;
    this.newSubTaskTitle = '';

    this.taskService.createSubTask(this.taskId, { title: titleToSend }).subscribe({
      next: (res: any) => {
        const newItem = res && res.id ? res : { id: res, title: titleToSend, isCompleted: false };
        this.taskDetail?.subTasks?.push(newItem);
        this.toaster.success('Thêm công việc con thành công.', 'Thông báo');
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.toaster.error(err.error?.error?.message || 'Không thể thêm công việc con!', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  toggleSubTask(st: any): void {
    const previousState = st.isCompleted;
    st.isCompleted = !st.isCompleted;

    this.taskService.updateSubTask(st.id, { title: st.title, isCompleted: st.isCompleted }).subscribe({
      error: (err: any) => {
        st.isCompleted = previousState;
        this.toaster.error(err.error?.error?.message || 'Không thể cập nhật!', 'Lỗi');
        this.cdr.detectChanges();
      }
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
    const oldTitle = st.title;
    st.title = this.editingSubTaskTitle;
    this.cancelEditSubTask();

    this.taskService.updateSubTask(st.id, { title: st.title, isCompleted: st.isCompleted }).subscribe({
      error: (err: any) => {
        st.title = oldTitle;
        this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật công việc con.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  deleteSubTask(id: string): void {
    this.taskService.deleteSubTask(id).subscribe({
      next: () => {
        if (this.taskDetail?.subTasks) {
          this.taskDetail.subTasks = this.taskDetail.subTasks.filter(x => x.id !== id);
        }
        this.toaster.success('Đã xóa công việc con.', 'Thông báo');
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.toaster.error(err.error?.error?.message || 'Không thể xóa công việc con!', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- CHECKLIST ---
  addChecklistItem(): void {
    const title = this.newChecklistTitle.trim();
    if (!title || !this.taskDetail) return;
    if (!this.taskDetail.checklistItems) this.taskDetail.checklistItems = [];

    const titleToSend = title;
    this.newChecklistTitle = '';

    this.taskService.createChecklistItem(this.taskId, { title: titleToSend }).subscribe({
      next: (res: any) => {
        const newItem = res && res.id ? res : { id: res, title: titleToSend, isDone: false };
        this.taskDetail?.checklistItems?.push(newItem);
        this.toaster.success('Đã thêm mục kiểm tra.', 'Thông báo');
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.toaster.error(err.error?.error?.message || 'Không thể thêm mục kiểm tra!', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  toggleChecklistItem(item: any): void {
    const previousState = item.isDone;
    item.isDone = !item.isDone;

    this.taskService.updateChecklistItem(item.id, { title: item.title, isDone: item.isDone }).subscribe({
      error: (err: any) => {
        item.isDone = previousState;
        this.toaster.error(err.error?.error?.message || 'Không thể cập nhật mục kiểm tra!', 'Lỗi');
        this.cdr.detectChanges();
      }
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
    const oldTitle = item.title;
    item.title = this.editingChecklistTitle;
    this.cancelEditChecklist();

    this.taskService.updateChecklistItem(item.id, { title: item.title, isDone: item.isDone }).subscribe({
      error: (err: any) => {
        item.title = oldTitle;
        this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật mục kiểm tra.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  deleteChecklistItem(id: string): void {
    this.taskService.deleteChecklistItem(id).subscribe({
      next: () => {
        if (this.taskDetail?.checklistItems) {
          this.taskDetail.checklistItems = this.taskDetail.checklistItems.filter(x => x.id !== id);
        }
        this.toaster.success('Đã xóa mục kiểm tra.', 'Thông báo');
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.toaster.error(err.error?.error?.message || 'Không thể xóa mục kiểm tra!', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  getChecklistProgress(): number {
    const list = this.taskDetail?.checklistItems;
    if (!list || list.length === 0) return 0;
    const doneCount = list.filter(i => i.isDone).length;
    return Math.round((doneCount / list.length) * 100);
  }

  // --- TRẠNG THÁI & HIỂN THỊ ---
  isStatusInProgress(): boolean {
    if (!this.taskDetail) return false;
    const st = String(this.taskDetail.status).toUpperCase();
    return st === '1' || st === 'INPROGRESS' || st === 'ĐANG LÀM';
  }

  isStatusInReview(): boolean {
    if (!this.taskDetail) return false;
    const st = String(this.taskDetail.status).toUpperCase();
    return st === '2' || st === 'INREVIEW' || st === 'CHỜ DUYỆT';
  }

  isStatusCompleted(): boolean {
    if (!this.taskDetail) return false;
    const st = String(this.taskDetail.status).toUpperCase();
    return st === '3' || st === 'COMPLETED' || st === 'HOÀN THÀNH';
  }

  // --- PHÂN QUYỀN VAI TRÒ (ROLE-BASED AUTHORIZATION) ---
  canSubmitReview(): boolean {
    if (!this.taskDetail || !this.isStatusInProgress()) return false;
    const raw = this.taskDetail as any;
    const assigneeId = raw.assigneeId || raw.AssigneeId || raw.assignee?.id;
    return !this.currentUserId || !assigneeId || this.currentUserId === assigneeId;
  }

  canApproveOrReject(): boolean {
    if (!this.taskDetail || !this.isStatusInReview()) return false;
    const raw = this.taskDetail as any;
    const assignorId = raw.assignorId || raw.AssignorId || raw.assignor?.id || raw.creatorId;
    return !this.currentUserId || !assignorId || this.currentUserId === assignorId;
  }

  get submissionNoteText(): string {
    if (this.localSubmittedNote) return this.localSubmittedNote;
    if (!this.taskDetail) return '';
    const raw = this.taskDetail as any;
    return (raw.submissionNote || raw.SubmissionNote || raw.reportNote || raw.resultNote || raw.submissionComment || raw.reportComment || raw.note || raw.result || '').toString().trim();
  }

  get reportFilesList(): TaskFileDto[] {
    if (this.localSubmittedFiles.length > 0) return this.localSubmittedFiles;
    if (!this.taskDetail) return [];

    const raw = this.taskDetail as any;
    const fileArray = raw.submissionFiles || raw.SubmissionFiles || raw.reportFiles || raw.resultFiles || raw.taskFiles || raw.attachments || raw.files;

    if (Array.isArray(fileArray) && fileArray.length > 0) return fileArray;

    const subUrls = raw.submissionFileUrl || raw.SubmissionFileUrl || raw.reportFileUrl || raw.submissionFilePath || raw.reportFilePath;
    const subNames = raw.submissionFileName || raw.SubmissionFileName || raw.reportFileName || raw.fileName;

    if (subUrls) {
      const urls: string[] = subUrls.split(';').filter(Boolean);
      const names: string[] = (subNames || '').split(';').filter(Boolean);
      return urls.map((url: string, idx: number): TaskFileDto => ({
        fileUrl: url,
        fileName: names[idx] || url.split('/').pop() || `Tep_BaoCao_${idx + 1}`
      }));
    }
    return [];
  }

  get shouldShowReportSection(): boolean {
    if (this.isSubmittedLocally) return true;
    if (!this.taskDetail) return false;
    return this.isStatusInReview() || this.isStatusCompleted() || !!this.submissionNoteText || this.reportFilesList.length > 0;
  }

  get assigneeDisplayName(): string {
    if (!this.taskDetail) return 'Chưa phân công';
    const raw = this.taskDetail as any;
    return raw.assigneeName || raw.AssigneeName || raw.assignee?.name || raw.assignee?.fullName || raw.userName || 'Chưa phân công';
  }

  get dueDateDisplay(): string | null {
    if (!this.taskDetail) return null;
    const raw = this.taskDetail as any;
    return raw.dueDate || raw.DueDate || raw.deadline || raw.endDate || raw.expireTime || null;
  }

  get originalFilesList(): TaskFileDto[] {
    if (!this.taskDetail) return [];
    const raw = this.taskDetail as any;

    if (Array.isArray(raw.originalFiles) && raw.originalFiles.length > 0) {
      return raw.originalFiles;
    }

    if (raw.fileUrl && !raw.submissionFileUrl) {
      const urls: string[] = raw.fileUrl.split(';').filter(Boolean);
      const names: string[] = (raw.fileName || '').split(';').filter(Boolean);
      return urls.map((url: string, idx: number): TaskFileDto => ({
        fileUrl: url,
        fileName: names[idx] || `YeuCau_DinhKem_${idx + 1}`
      }));
    }
    return [];
  }

  getFileName(file: TaskFileDto | any): string {
    let name = '';
    if (typeof file === 'string') {
      name = file.split('/').pop() || 'Tệp đính kèm';
    } else {
      name = file?.fileName || file?.name || file?.originalFileName || 'Tệp đính kèm';
    }
    return name.replace(/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}_?/i, '');
  }

  getFileUrl(file: TaskFileDto | any): SafeUrl | string {
    if (!file) return '#';
    if (typeof file === 'string') {
      return file.startsWith('data:') ? this.sanitizer.bypassSecurityTrustUrl(file) : file;
    }
    if (file.fileUrl) {
      return file.fileUrl.startsWith('data:') ? this.sanitizer.bypassSecurityTrustUrl(file.fileUrl) : file.fileUrl;
    }
    if (file.url) return file.url;
    if (file.filePath) return file.filePath;
    if (file.fileContent) {
      const dataUrl = `data:application/octet-stream;base64,${file.fileContent}`;
      return this.sanitizer.bypassSecurityTrustUrl(dataUrl);
    }
    return '#';
  }

  getFileIcon(file: TaskFileDto | any): string {
    const fileName = this.getFileName(file);
    const ext = fileName.split('.').pop()?.toLowerCase();
    switch (ext) {
      case 'pdf': return 'bi-file-earmark-pdf text-danger';
      case 'doc': case 'docx': return 'bi-file-earmark-word text-primary';
      case 'xls': case 'xlsx': return 'bi-file-earmark-excel text-success';
      case 'jpg': case 'png': case 'jpeg': return 'bi-file-earmark-image text-warning';
      case 'zip': case 'rar': return 'bi-file-earmark-zip text-secondary';
      default: return 'bi-file-earmark-text text-secondary';
    }
  }

  openFile(fileUrl?: any): void {
    if (!fileUrl || fileUrl === '#') return;
    const urlString = typeof fileUrl === 'string' ? fileUrl : (fileUrl.changingThisBreaksDevSecurity || '#');
    if (urlString !== '#') {
      window.open(urlString, '_blank');
    }
  }

  // --- MODALS & SUBMIT ---
  openSubmitModal(): void {
    this.isSubmitModalOpen = true;
    document.body.classList.add('modal-open');
    this.cdr.detectChanges();
  }

  closeSubmitModal(): void {
    this.isSubmitModalOpen = false;
    document.body.classList.remove('modal-open');
    this.submissionNote = '';
    this.submissionFiles = [];
    if (this.submitFileInput) {
      this.submitFileInput.nativeElement.value = '';
    }
    this.cdr.detectChanges();
  }

  openRejectModal(): void {
    this.isRejectModalOpen = true;
    document.body.classList.add('modal-open');
    this.rejectReason = '';
    this.cdr.detectChanges();
  }

  closeRejectModal(): void {
    this.isRejectModalOpen = false;
    document.body.classList.remove('modal-open');
    this.rejectReason = '';
    this.cdr.detectChanges();
  }

  onSubmissionFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      Array.from(input.files).forEach(file => this.submissionFiles.push(file));
      input.value = '';
    }
  }

  removeSubmissionFile(index: number): void {
    this.submissionFiles.splice(index, 1);
  }

  private fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        const result = reader.result as string;
        const base64Data = result.includes(',') ? result.split(',')[1] : result;
        resolve(base64Data);
      };
      reader.onerror = (err) => reject(err);
    });
  }

  async confirmSubmitForReview(): Promise<void> {
    const cleanNote = this.submissionNote.trim();
    if (!cleanNote && this.submissionFiles.length === 0) {
      this.toaster.warn('Vui lòng nhập ghi chú hoặc đính kèm tệp báo cáo!', 'Thông báo');
      return;
    }

    this.isActionLoading = true;

    try {
      const previewFiles = this.submissionFiles.map(file => ({
        fileName: file.name,
        fileUrl: URL.createObjectURL(file)
      }));

      const mappedAttachments = await Promise.all(
        this.submissionFiles.map(async (file) => ({
          fileName: file.name,
          fileContent: await this.fileToBase64(file)
        }))
      );

      const payload = {
        note: cleanNote,
        attachments: mappedAttachments
      };

      this.taskService.submitForReview(this.taskId, payload).subscribe({
        next: () => {
          this.localSubmittedNote = cleanNote;
          this.localSubmittedFiles = previewFiles;
          this.isSubmittedLocally = true;

          if (this.taskDetail) {
            this.taskDetail.status = TaskStatus.InReview;
          }

          this.isActionLoading = false;
          this.closeSubmitModal();
          this.toaster.success('Đã nộp báo cáo kết quả thành công!', 'Thông báo');
          this.cdr.detectChanges();
          this.loadTaskDetail(true);
        },
        error: (err: any) => {
          this.isActionLoading = false;
          this.toaster.error(err.error?.error?.message || 'Có lỗi xảy ra khi nộp báo cáo.', 'Lỗi');
          this.cdr.detectChanges();
        }
      });
    } catch {
      this.isActionLoading = false;
      this.toaster.error('Lỗi mã hóa tệp đính kèm.', 'Lỗi');
      this.cdr.detectChanges();
    }
  }

  approveTask(): void {
    this.isActionLoading = true;
    this.taskService.approveTask(this.taskId).subscribe({
      next: () => {
        this.isActionLoading = false;
        this.toaster.success('Đã phê duyệt công việc thành công!', 'Thông báo');
        this.loadTaskDetail(true);
      },
      error: (err: any) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Không thể phê duyệt công việc.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  confirmRejectTask(): void {
    const reason = this.rejectReason.trim();
    if (!reason) {
      this.toaster.warn('Vui lòng nhập lý do từ chối!', 'Thông báo');
      return;
    }

    this.isActionLoading = true;
    this.taskService.rejectTask(this.taskId, { reason: reason }).subscribe({
      next: () => {
        this.isActionLoading = false;
        this.closeRejectModal();
        this.toaster.warn('Đã từ chối công việc.', 'Thông báo');
        this.loadTaskDetail(true);
      },
      error: (err: any) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Không thể từ chối công việc!', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  changeStatus(status: TaskStatus): void {
    this.isActionLoading = true;
    this.taskService.updateStatus(this.taskId, status).subscribe({
      next: () => {
        this.isActionLoading = false;
        this.toaster.success('Cập nhật trạng thái thành công.', 'Thông báo');
        this.loadTaskDetail(true);
      },
      error: (err: any) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật trạng thái.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  getStatusBadgeClass(status: TaskStatus | any): string {
    const st = String(status).toUpperCase();
    if (st === '0' || st === 'NEW' || st === 'MỚI') return 'bg-secondary text-white';
    if (st === '1' || st === 'INPROGRESS' || st === 'ĐANG LÀM') return 'bg-primary text-white';
    if (st === '2' || st === 'INREVIEW' || st === 'CHỜ DUYỆT') return 'bg-warning text-dark';
    if (st === '3' || st === 'COMPLETED' || st === 'HOÀN THÀNH') return 'bg-success text-white';
    if (st === '4' || st === 'CANCELLED' || st === 'ĐÃ HỦY') return 'bg-danger text-white';
    return 'bg-light text-dark';
  }

  getStatusText(status: TaskStatus | any): string {
    const st = String(status).toUpperCase();
    if (st === '0' || st === 'NEW') return 'Mới';
    if (st === '1' || st === 'INPROGRESS') return 'Đang làm';
    if (st === '2' || st === 'INREVIEW') return 'Chờ duyệt';
    if (st === '3' || st === 'COMPLETED') return 'Hoàn thành';
    if (st === '4' || st === 'CANCELLED') return 'Đã hủy';
    return String(status || 'N/A');
  }

  getPriorityBadgeClass(priority: TaskPriority | any): string {
    const pr = String(priority).toUpperCase();
    if (pr === '0' || pr === 'LOW') return 'bg-secondary text-white';
    if (pr === '1' || pr === 'MEDIUM') return 'bg-info text-white';
    if (pr === '2' || pr === 'HIGH') return 'bg-danger text-white';
    if (pr === '3' || pr === 'URGENT') return 'bg-dark text-white';
    return 'bg-light text-dark';
  }

  getPriorityText(priority: TaskPriority | any): string {
    const pr = String(priority).toUpperCase();
    if (pr === '0' || pr === 'LOW') return 'Thấp';
    if (pr === '1' || pr === 'MEDIUM') return 'Trung bình';
    if (pr === '2' || pr === 'HIGH') return 'Cao';
    if (pr === '3' || pr === 'URGENT') return 'Khẩn cấp';
    return 'Thường';
  }
}