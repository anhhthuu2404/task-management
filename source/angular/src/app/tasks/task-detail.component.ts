import { Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ToasterService } from '@abp/ng.theme.shared';
import { ConfigStateService } from '@abp/ng.core';
import { of, Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { 
  TaskService, 
  TaskStatus, 
  TaskPriority, 
  TaskDetailDto, 
  SubmitReportInput,
  CreateTaskCommentDto,
  CommentAttachmentDto,
  TaskCommentDto
} from './task.service';

export interface LocalTaskDetailDto extends TaskDetailDto {
  fileUrl?: string;
  fileName?: string;
  submissionFileName?: string;
  submissionFileUrl?: string;
  submissionNote?: string;
  note?: string;
  submissionFiles?: { fileName?: string; name?: string; fileUrl?: string; url?: string }[];
  
  // Bổ sung các trường ID phục vụ phân quyền (nếu backend trả về)
  assigneeId?: string;
  assignedToUserId?: string;
  creatorId?: string;
  managerId?: string;
}

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styles: [`
    .status-dropdown-container .dropdown-menu.show { display: block; }
    .nav-tabs .nav-link { cursor: pointer; }
    .modal.show { display: block; background: rgba(0, 0, 0, 0.5); }
    .fs-7 { font-size: 0.85rem; }
    .action-btn { cursor: pointer; opacity: 0.7; transition: 0.2s; }
    .action-btn:hover { opacity: 1; }
    .cursor-pointer { cursor: pointer; }
  `],
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule]
})
export class TaskDetailComponent implements OnInit {
  @ViewChild('submitFileInput') submitFileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('commentFileInput') commentFileInput!: ElementRef<HTMLInputElement>;

  taskId: string = '';
  taskDetail: LocalTaskDetailDto | null = null;
  isLoading: boolean = false;
  isActionLoading: boolean = false;
  currentUserId: string | null = null;

  // --- PHÂN QUYỀN VAI TRÒ (BỔ SUNG) ---
  get isAssignee(): boolean {
    if (!this.currentUserId || !this.taskDetail) return false;
    return this.taskDetail.assigneeId === this.currentUserId || 
           this.taskDetail.assignedToUserId === this.currentUserId;
  }

  get isCreatorOrManager(): boolean {
    if (!this.currentUserId || !this.taskDetail) return false;
    return this.taskDetail.creatorId === this.currentUserId || 
           this.taskDetail.managerId === this.currentUserId;
  }

  // Dropdown Trạng thái
  isStatusDropdownOpen: boolean = false;

  // Tab State
  activeTab: 'comments' | 'subtask' | 'checklist' | 'timeline' = 'comments';

  // Comment State
  comments: TaskCommentDto[] = [];
  newCommentText: string = '';
  commentSelectedFiles: File[] = [];
  isSubmittingComment: boolean = false;

  // Comment Inline Editing State
  editingCommentId: string | null = null;
  editingCommentText: string = '';

  // --- TIMELINE STATE ---
  timelineLogs: any[] = [];
  isLoadingTimeline: boolean = false;

  // Modals State
  isSubmitModalOpen: boolean = false;
  isEditSubmissionMode: boolean = false;
  submissionNote: string = '';
  selectedSubmitFiles: { fileName: string; fileContent: string }[] = [];

  isRejectModalOpen: boolean = false;
  rejectReason: string = '';

  // --- CÔNG VIỆC PHỤ (SUBTASKS) ---
  isAddSubTaskOpen: boolean = false;
  newSubTaskTitle: string = '';
  subTaskList: { title: string; completed: boolean }[] = [
    { title: 'Khảo sát yêu cầu hệ thống', completed: true },
    { title: 'Thiết kế cơ sở dữ liệu và API', completed: false }
  ];

  // --- CHECKLIST STATE ---
  isAddChecklistOpen: boolean = false;
  newChecklistTitle: string = '';
  checklistItems: { id: string; title: string; completed: boolean }[] = [
    { id: '1', title: 'Kiểm tra tính hợp lệ của dữ liệu đầu vào', completed: true },
    { id: '2', title: 'Viết tài liệu hướng dẫn sử dụng', completed: true }
  ];

  readonly TaskStatus = TaskStatus;
  readonly TaskPriority = TaskPriority;

  constructor(
    private route: ActivatedRoute,
    private taskService: TaskService,
    private toaster: ToasterService,
    private cdr: ChangeDetectorRef,
    private location: Location,
    private configState: ConfigStateService
  ) {}

  ngOnInit(): void {
    const currentUser = this.configState.getOne('currentUser') as { id?: string; name?: string; userName?: string };
    this.currentUserId = currentUser?.id || null;

    this.taskId = this.route.snapshot.paramMap.get('id') || '';
    if (this.taskId) {
      this.loadTaskDetail();
    } else {
      this.isLoading = false;
      this.toaster.error('Không tìm thấy mã công việc!', 'Lỗi');
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.status-dropdown-container')) {
      this.isStatusDropdownOpen = false;
    }
  }

  // --- XỬ LÝ CHUYỂN TAB ---
  switchTab(tab: 'comments' | 'subtask' | 'checklist' | 'timeline'): void {
    this.activeTab = tab;
    if (tab === 'timeline') {
      this.loadTimelineLogs();
    }
  }

  // --- TẢI LỊCH SỬ HOẠT ĐỘNG (TIMELINE) ---
  loadTimelineLogs(): void {
    if (!this.taskId) return;
    this.isLoadingTimeline = true;

    if (typeof (this.taskService as any).getTaskTimeline === 'function') {
      (this.taskService as any).getTaskTimeline(this.taskId).subscribe({
        next: (res: any[]) => {
          this.timelineLogs = res || [];
          this.isLoadingTimeline = false;
          this.cdr.detectChanges();
        },
        error: (err: { error?: { error?: { message?: string } } }) => {
          this.isLoadingTimeline = false;
          this.toaster.error(err.error?.error?.message || 'Không thể tải lịch sử hoạt động.', 'Lỗi');
          this.cdr.detectChanges();
        }
      });
    } else {
      this.isLoadingTimeline = false;
      this.timelineLogs = [];
      this.cdr.detectChanges();
    }
  }

  toggleStatusDropdown(event: Event): void {
    event.stopPropagation();
    this.isStatusDropdownOpen = !this.isStatusDropdownOpen;
  }

  changeStatus(status: TaskStatus): void {
    this.isStatusDropdownOpen = false;
    this.isActionLoading = true;

    this.taskService.updateStatus(this.taskId, status).subscribe({
      next: () => {
        this.isActionLoading = false;
        if (this.taskDetail) {
          this.taskDetail.status = status;
        }
        this.toaster.success('Cập nhật trạng thái thành công.', 'Thông báo');
        this.loadTaskDetail(true);
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật trạng thái.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- XÓA CÔNG VIỆC ---
  onDeleteTask(): void {
    if (!confirm('Bạn có chắc chắn muốn xóa vĩnh viễn công việc này không?')) {
      return;
    }

    this.isActionLoading = true;
    this.taskService.deleteTask(this.taskId).subscribe({
      next: () => {
        this.isActionLoading = false;
        this.toaster.success('Đã xóa công việc thành công.', 'Thông báo');
        this.goBack();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi xóa công việc.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- BÌNH LUẬN & ĐÍNH KÈM FILE ---
  onCommentFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      for (let i = 0; i < input.files.length; i++) {
        this.commentSelectedFiles.push(input.files[i]);
      }
      this.cdr.detectChanges();
    }
  }

  removeCommentFile(index: number): void {
    this.commentSelectedFiles.splice(index, 1);
    if (this.commentFileInput) {
      this.commentFileInput.nativeElement.value = '';
    }
  }

  private convertFileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = (error) => reject(error);
    });
  }

  async sendComment(): Promise<void> {
    if (!this.newCommentText.trim() && this.commentSelectedFiles.length === 0) {
      return;
    }

    this.isSubmittingComment = true;

    try {
      const currentUser = this.configState.getOne('currentUser') as { id?: string; name?: string; userName?: string };
      const attachmentsPromises: Promise<CommentAttachmentDto>[] = this.commentSelectedFiles.map(async file => ({
        fileName: file.name,
        fileContent: await this.convertFileToBase64(file)
      }));

      const attachments = await Promise.all(attachmentsPromises);

      const payload: CreateTaskCommentDto = {
        text: this.newCommentText,
        attachments: attachments
      };

      this.taskService.createComment(this.taskId, payload).subscribe({
        next: (createdComment: TaskCommentDto) => {
          this.newCommentText = '';
          this.commentSelectedFiles = [];
          if (this.commentFileInput) {
            this.commentFileInput.nativeElement.value = '';
          }
          this.isSubmittingComment = false;
          this.toaster.success('Đã gửi bình luận.', 'Thông báo');

          const newCommentObj: TaskCommentDto = {
            id: createdComment?.id || new Date().getTime().toString(),
            taskId: this.taskId,
            text: createdComment?.text || payload.text,
            creatorId: createdComment?.creatorId || currentUser?.id,
            creationTime: createdComment?.creationTime || new Date().toISOString(),
            creatorName: createdComment?.creatorName || currentUser?.name || currentUser?.userName || 'Tôi',
            attachments: (createdComment?.attachments && createdComment.attachments.length > 0) 
              ? createdComment.attachments 
              : (payload.attachments || [])
          };

          this.comments = [newCommentObj, ...(this.comments || [])];
          if (this.taskDetail) {
            this.taskDetail.comments = [...this.comments];
          }

          this.cdr.detectChanges();
        },
        error: (err: { error?: { error?: { message?: string } } }) => {
          this.isSubmittingComment = false;
          this.toaster.error(err.error?.error?.message || 'Lỗi gửi bình luận.', 'Lỗi');
          this.cdr.detectChanges();
        }
      });
    } catch {
      this.isSubmittingComment = false;
      this.toaster.error('Lỗi khi xử lý file đính kèm.', 'Lỗi');
      this.cdr.detectChanges();
    }
  }

  // --- SỬA & XÓA BÌNH LUẬN ---
  startEditComment(comment: TaskCommentDto): void {
    this.editingCommentId = comment.id || null;
    this.editingCommentText = comment.text;
  }

  cancelEditComment(): void {
    this.editingCommentId = null;
    this.editingCommentText = '';
  }

  saveEditComment(commentId?: string): void {
    if (!commentId || !this.editingCommentText.trim()) return;

    this.taskService.updateComment(commentId, { text: this.editingCommentText }).subscribe({
      next: () => {
        const item = this.comments.find(c => c.id === commentId);
        if (item) {
          item.text = this.editingCommentText;
        }
        if (this.taskDetail) {
          this.taskDetail.comments = [...this.comments];
        }
        this.editingCommentId = null;
        this.editingCommentText = '';
        this.toaster.success('Đã cập nhật bình luận.', 'Thành công');
        this.cdr.detectChanges();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.toaster.error(err.error?.error?.message || 'Lỗi khi sửa bình luận.', 'Lỗi');
      }
    });
  }

  deleteComment(commentId?: string): void {
    if (!commentId) return;
    if (!confirm('Bạn có chắc chắn muốn xóa bình luận này không?')) return;

    this.taskService.deleteComment(commentId).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.id !== commentId);
        
        if (this.taskDetail) {
          this.taskDetail.comments = [...this.comments];
        }

        this.toaster.success('Đã xóa bình luận.', 'Thông báo');
        this.cdr.detectChanges();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.toaster.error(err.error?.error?.message || 'Lỗi khi xóa bình luận.', 'Lỗi');
      }
    });
  }

  // --- DUYỆT CÔNG VIỆC ---
  approveTask(): void {
    this.isActionLoading = true;
    this.taskService.approveTask(this.taskId).subscribe({
      next: () => {
        this.isActionLoading = false;
        if (this.taskDetail) {
          this.taskDetail.status = TaskStatus.Completed;
        }
        this.toaster.success('Đã duyệt và hoàn thành công việc!', 'Thành công');
        this.loadTaskDetail(true);
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi duyệt công việc.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- NỘP BÁO CÁO & XÓA SỬA NỘP BÀI ---
  openSubmitModal(isEdit: boolean = false): void {
    this.isEditSubmissionMode = isEdit;
    this.submissionNote = this.taskDetail?.submissionNote || this.taskDetail?.note || '';
    this.selectedSubmitFiles = [];
    this.isSubmitModalOpen = true;
  }

  closeSubmitModal(): void {
    this.isSubmitModalOpen = false;
    this.isEditSubmissionMode = false;
  }

  onSubmissionFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const files = Array.from(input.files);
    this.selectedSubmitFiles = [];

    files.forEach(file => {
      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        this.selectedSubmitFiles.push({
          fileName: file.name,
          fileContent: (e.target?.result as string) || ''
        });
        this.cdr.detectChanges();
      };
      reader.readAsDataURL(file);
    });
  }

  confirmSubmitForReview(): void {
    if (!this.submissionNote.trim() && this.selectedSubmitFiles.length === 0 && !this.isEditSubmissionMode) {
      this.toaster.warn('Vui lòng nhập ghi chú hoặc đính kèm tệp báo cáo!', 'Cảnh báo');
      return;
    }

    this.isActionLoading = true;

    const inputPayload: SubmitReportInput = {
      note: this.submissionNote,
      attachments: this.selectedSubmitFiles
    };

    if (this.isEditSubmissionMode) {
      this.taskService.updateSubmission(this.taskId, inputPayload).subscribe({
        next: () => {
          this.isActionLoading = false;
          this.closeSubmitModal();
          this.toaster.success('Đã cập nhật mục nộp bài duyệt!', 'Thành công');
          this.loadTaskDetail(true);
        },
        error: (err: { error?: { error?: { message?: string } } }) => {
          this.isActionLoading = false;
          this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật bài nộp.', 'Lỗi');
          this.cdr.detectChanges();
        }
      });
    } else {
      const prepare$: Observable<void> = (this.taskDetail?.status === TaskStatus.New)
        ? this.taskService.updateStatus(this.taskId, TaskStatus.InProgress)
        : of(undefined);

      prepare$.pipe(
        switchMap(() => this.taskService.submitForReview(this.taskId, inputPayload))
      ).subscribe({
        next: () => {
          this.isActionLoading = false;
          this.closeSubmitModal();
          this.toaster.success('Đã nộp báo cáo và gửi duyệt thành công!', 'Thành công');
          this.loadTaskDetail(true);
        },
        error: (err: { error?: { error?: { message?: string } } }) => {
          this.isActionLoading = false;
          this.toaster.error(err.error?.error?.message || 'Lỗi khi nộp bài duyệt.', 'Lỗi');
          this.cdr.detectChanges();
        }
      });
    }
  }

  deleteSubmission(): void {
    if (!confirm('Bạn có chắc chắn muốn xóa/hủy lượt nộp báo cáo này không?')) return;

    this.isActionLoading = true;
    this.taskService.deleteSubmission(this.taskId).subscribe({
      next: () => {
        this.isActionLoading = false;
        this.toaster.success('Đã xóa lượt nộp báo cáo.', 'Thông báo');
        this.loadTaskDetail(true);
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi xóa bài nộp.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- TỪ CHỐI DUYỆT ---
  openRejectModal(): void {
    this.rejectReason = '';
    this.isRejectModalOpen = true;
  }

  closeRejectModal(): void {
    this.isRejectModalOpen = false;
  }

  confirmRejectTask(): void {
    if (!this.rejectReason.trim()) return;

    this.isActionLoading = true;
    this.taskService.rejectTask(this.taskId, { reason: this.rejectReason }).subscribe({
      next: () => {
        this.isActionLoading = false;
        this.closeRejectModal();
        this.toaster.warn('Đã từ chối duyệt công việc.', 'Thông báo');
        this.loadTaskDetail(true);
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi xử lý từ chối.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // --- CÔNG VIỆC PHỤ (SUBTASKS) ---
  openAddSubTaskModal(): void {
    this.isAddSubTaskOpen = true;
    this.newSubTaskTitle = '';
    this.cdr.detectChanges();
  }

  saveNewSubTask(): void {
    if (!this.newSubTaskTitle || !this.newSubTaskTitle.trim()) return;
    
    this.subTaskList.push({
      title: this.newSubTaskTitle.trim(),
      completed: false
    });
    
    this.newSubTaskTitle = '';
    this.isAddSubTaskOpen = false;
    this.cdr.detectChanges();
  }

  deleteSubTask(index: number): void {
    this.subTaskList.splice(index, 1);
    this.cdr.detectChanges();
  }

  // --- CHECKLIST ---
  addChecklistItem(): void {
    if (!this.newChecklistTitle || !this.newChecklistTitle.trim()) return;

    this.checklistItems.push({
      id: new Date().getTime().toString(),
      title: this.newChecklistTitle.trim(),
      completed: false
    });

    this.newChecklistTitle = '';
    this.cdr.detectChanges();
  }

  toggleChecklistItem(item: { id: string; title: string; completed: boolean }): void {
    item.completed = !item.completed;
    this.cdr.detectChanges();
  }

  deleteChecklistItem(index: number): void {
    this.checklistItems.splice(index, 1);
    this.cdr.detectChanges();
  }

  goBack(): void {
    this.location.back();
  }

  loadTaskDetail(isSilent: boolean = false): void {
    if (!isSilent) this.isLoading = true;
    this.taskService.getTaskDetail(this.taskId).subscribe({
      next: (data: TaskDetailDto & { comments?: TaskCommentDto[]; taskComments?: TaskCommentDto[] }) => {
        this.taskDetail = data as LocalTaskDetailDto;
        
        const loadedComments = data.comments || data.taskComments || [];
        this.comments = loadedComments;
        
        if (this.taskDetail) {
          this.taskDetail.comments = [...this.comments];
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isLoading = false;
        this.toaster.error(err.error?.error?.message || 'Không thể tải thông tin công việc!', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  getStatusBadgeClass(status: TaskStatus | number): string {
    const st = Number(status);
    if (st === TaskStatus.New) return 'bg-secondary text-white';
    if (st === TaskStatus.InProgress) return 'bg-primary text-white';
    if (st === TaskStatus.InReview) return 'bg-warning text-dark';
    if (st === TaskStatus.Completed) return 'bg-success text-white';
    if (st === TaskStatus.Cancelled) return 'bg-danger text-white';
    return 'bg-light text-dark';
  }

  getStatusText(status: TaskStatus | number): string {
    const st = Number(status);
    if (st === TaskStatus.New) return 'Mới';
    if (st === TaskStatus.InProgress) return 'Đang làm';
    if (st === TaskStatus.InReview) return 'Chờ duyệt';
    if (st === TaskStatus.Completed) return 'Hoàn thành';
    if (st === TaskStatus.Cancelled) return 'Đã hủy';
    return String(status || 'N/A');
  }

  downloadFile(file: { fileUrl?: string; url?: string; fileName?: string; name?: string }): void {
    const fileUrl = file?.fileUrl || file?.url;
    if (!fileUrl) {
      this.toaster.error('Không tìm thấy đường dẫn của tệp này!', 'Lỗi');
      return;
    }

    const link = document.createElement('a');
    link.href = fileUrl;
    link.target = '_blank';
    
    const fileName = file?.fileName || file?.name;
    if (fileName) {
      link.download = fileName;
    }

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  trackByCommentId(index: number, item: TaskCommentDto): string | number {
    return item?.id || index;
  }

  trackBySubTaskIndex(index: number): number {
    return index;
  }
}