import { Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ToasterService } from '@abp/ng.theme.shared';
import { ConfigStateService } from '@abp/ng.core';
import { of, Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { CoreModule } from '@abp/ng.core';
import { LocalizationService } from '@abp/ng.core';
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

export interface LocalTaskDetailDto extends Omit<TaskDetailDto, 'checklistItems' | 'subTasks'> {
  fileUrl?: string;
  fileName?: string;
  submissionFileName?: string;
  submissionFileUrl?: string;
  submissionNote?: string;
  note?: string;
  submissionFiles?: { fileName?: string; name?: string; fileUrl?: string; url?: string }[];
  
  // Phân quyền
  assigneeId?: string;
  assignedToUserId?: string;
  creatorId?: string;
  managerId?: string;
  
  // Lịch sử và concurrencyStamp
  histories?: any[];
  concurrencyStamp?: string;

  // Kiểu dữ liệu linh hoạt cho SubTasks và ChecklistItems khớp cơ sở dữ liệu
  subTasks?: any[];
  checklistItems?: any[];
  checklists?: any[];
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
  imports: [CommonModule, CoreModule, FormsModule, RouterModule]
})
export class TaskDetailComponent implements OnInit {
  @ViewChild('submitFileInput') submitFileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('commentFileInput') commentFileInput!: ElementRef<HTMLInputElement>;

  taskId: string = '';
  taskDetail: LocalTaskDetailDto | null = null;
  isLoading: boolean = false;
  isActionLoading: boolean = false;
  currentUserId: string | null = null;

  usersList: { id: string; name?: string; userName?: string }[] = [];

  get isAssignee(): boolean {
    if (!this.currentUserId || !this.taskDetail) return false;
    return this.taskDetail.assigneeId === this.currentUserId || 
           this.taskDetail.assignedToUserId === this.currentUserId;
  }
  

  get isCreatorOrManager(): boolean {
    if (!this.currentUserId || !this.taskDetail) return false;
    
    const currentUser = this.configState.getOne('currentUser') as { userName?: string; roles?: string[] };
    const isAdmin = currentUser?.userName === 'admin' || currentUser?.roles?.includes('admin') || currentUser?.roles?.includes('Admin');

    return isAdmin || 
           this.taskDetail.creatorId === this.currentUserId || 
           this.taskDetail.managerId === this.currentUserId;
  }

  isStatusDropdownOpen: boolean = false;
  activeTab: 'comments' | 'subtask' | 'checklist' | 'timeline' = 'comments';

  comments: TaskCommentDto[] = [];
  newCommentText: string = '';
  commentSelectedFiles: File[] = [];
  isSubmittingComment: boolean = false;

  editingCommentId: string | null = null;
  editingCommentText: string = '';

  timelineLogs: any[] = [];
  isLoadingTimeline: boolean = false;

  isSubmitModalOpen: boolean = false;
  isEditSubmissionMode: boolean = false;
  submissionNote: string = '';
  selectedSubmitFiles: { fileName: string; fileContent: string }[] = [];

  isRejectModalOpen: boolean = false;
  rejectReason: string = '';

  isAddSubTaskOpen: boolean = false;
  newSubTaskTitle: string = '';
  subTaskList: { id: string; title: string; isCompleted: boolean; completed: boolean }[] = [];

  isAddChecklistOpen: boolean = false;
  newChecklistTitle: string = '';
  checklistItems: { id: string; title: string; isCompleted: boolean; completed: boolean }[] = [];

  readonly TaskStatus = TaskStatus;
  readonly TaskPriority = TaskPriority;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private taskService: TaskService,
    private toaster: ToasterService,
    private cdr: ChangeDetectorRef,
    private location: Location,
    private localizationService: LocalizationService,
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

  switchTab(tab: 'comments' | 'subtask' | 'checklist' | 'timeline'): void {
    this.activeTab = tab;
    if (tab === 'timeline') {
      this.loadTimelineLogs();
    }
  }

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
      this.timelineLogs = this.taskDetail?.histories || [];
      this.isLoadingTimeline = false;
      this.cdr.detectChanges();
    }
  }

  updateTaskAssignee(assigneeId: string | null): void {
    if (!this.taskId) return;

    this.isActionLoading = true;
    this.taskService.updateAssignee(this.taskId, assigneeId).subscribe({
      next: (updatedTask: any) => {
        this.isActionLoading = false;
        if (this.taskDetail && updatedTask) {
          this.taskDetail.assigneeId = updatedTask.assigneeId;
          this.taskDetail.assigneeName = updatedTask.assigneeName;
          if (updatedTask.concurrencyStamp) {
            this.taskDetail.concurrencyStamp = updatedTask.concurrencyStamp;
          }
        }
        this.toaster.success('Đã cập nhật người thực hiện thành công.', 'Thông báo');
        this.loadTaskDetail(true);
        this.loadTimelineLogs();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi đổi người thực hiện.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
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
        this.loadTimelineLogs();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật trạng thái.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

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

          this.loadTimelineLogs();
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
        this.loadTimelineLogs();
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
        this.loadTimelineLogs();
        this.cdr.detectChanges();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.toaster.error(err.error?.error?.message || 'Lỗi khi xóa bình luận.', 'Lỗi');
      }
    });
  }

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
        this.loadTimelineLogs();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi duyệt công việc.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

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
          this.loadTimelineLogs();
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
          this.loadTimelineLogs();
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
        this.loadTimelineLogs();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi xóa bài nộp.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

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
        this.loadTimelineLogs();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi xử lý từ chối.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  // ========================== CÔNG VIỆC CON (SUB-TASKS) ==========================

  openAddSubTaskModal(): void {
    this.isAddSubTaskOpen = true;
    this.newSubTaskTitle = '';
    this.cdr.detectChanges();
  }

  saveNewSubTask(): void {
    if (!this.newSubTaskTitle || !this.newSubTaskTitle.trim()) return;
    
    const title = this.newSubTaskTitle.trim();
    this.isActionLoading = true;

    const subTaskService: any = this.taskService;
    const createSubTask$ = typeof subTaskService.createSubTask === 'function'
      ? subTaskService.createSubTask(this.taskId, { title })
      : (typeof subTaskService.addSubTask === 'function' 
          ? subTaskService.addSubTask(this.taskId, { title }) 
          : of(null));

    createSubTask$.subscribe({
      next: () => {
        this.isActionLoading = false;
        this.newSubTaskTitle = '';
        this.isAddSubTaskOpen = false;
        this.toaster.success('Đã thêm công việc con thành công.', 'Thông báo');
        this.loadTaskDetail(true);
        this.loadTimelineLogs();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi thêm công việc con.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  toggleSubTaskItem(item: { id: string; title: string; isCompleted: boolean; completed: boolean }): void {
    item.completed = !item.completed;
    item.isCompleted = item.completed;
    
    const subTaskService: any = this.taskService;
    if (item.id && typeof subTaskService.toggleSubTaskStatus === 'function') {
      subTaskService.toggleSubTaskStatus(item.id).subscribe({
        next: () => {
          this.loadTimelineLogs();
        },
        error: (err: any) => {
          this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật trạng thái công việc con.', 'Lỗi');
          item.completed = !item.completed;
          item.isCompleted = item.completed;
          this.cdr.detectChanges();
        }
      });
    } else if (item.id && typeof subTaskService.updateSubTask === 'function') {
      subTaskService.updateSubTask(item.id, { title: item.title, isCompleted: item.completed }).subscribe({
        next: () => {
          this.loadTimelineLogs();
        },
        error: (err: any) => {
          this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật trạng thái công việc con.', 'Lỗi');
          item.completed = !item.completed;
          item.isCompleted = item.completed;
          this.cdr.detectChanges();
        }
      });
    }
    this.cdr.detectChanges();
  }

  deleteSubTask(index: number, subTaskId?: string): void {
    if (!subTaskId) {
      this.toaster.error('Không tìm thấy mã định danh (ID) của công việc con!', 'Lỗi');
      return;
    }
    if (!confirm('Bạn có chắc chắn muốn xóa công việc con này không?')) return;

    const subTaskService: any = this.taskService;
    if (typeof subTaskService.deleteSubTask === 'function') {
      this.isActionLoading = true;
      subTaskService.deleteSubTask(subTaskId).subscribe({
        next: () => {
          this.isActionLoading = false;
          this.toaster.success('Đã xóa công việc con.', 'Thông báo');
          this.loadTaskDetail(true);
          this.loadTimelineLogs();
        },
        error: (err: any) => {
          this.isActionLoading = false;
          this.toaster.error(err.error?.error?.message || 'Lỗi khi xóa công việc con.', 'Lỗi');
          this.cdr.detectChanges();
        }
      });
    } else {
      this.toaster.error('Hệ thống chưa hỗ trợ phương thức xóa công việc con.', 'Lỗi');
    }
  }

  // ========================== MỤC KIỂM TRA (CHECKLIST) ==========================

  addChecklistItem(): void {
    if (!this.newChecklistTitle || !this.newChecklistTitle.trim()) return;

    const title = this.newChecklistTitle.trim();
    this.isActionLoading = true;

    const taskSvc: any = this.taskService;
    const createChecklist$ = typeof taskSvc.createChecklistItem === 'function'
      ? taskSvc.createChecklistItem(this.taskId, { title })
      : (typeof taskSvc.addChecklist === 'function' 
          ? taskSvc.addChecklist(this.taskId, { title }) 
          : of(null));

    createChecklist$.subscribe({
      next: () => {
        this.isActionLoading = false;
        this.newChecklistTitle = '';
        this.toaster.success('Đã thêm mục kiểm tra thành công.', 'Thông báo');
        this.loadTaskDetail(true);
        this.loadTimelineLogs();
      },
      error: (err: { error?: { error?: { message?: string } } }) => {
        this.isActionLoading = false;
        this.toaster.error(err.error?.error?.message || 'Lỗi khi thêm mục kiểm tra.', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  toggleChecklistItem(item: { id: string; title: string; isCompleted: boolean; completed: boolean }): void {
    item.completed = !item.completed;
    item.isCompleted = item.completed;

    const taskSvc: any = this.taskService;
    if (item.id && typeof taskSvc.toggleChecklistItemStatus === 'function') {
      taskSvc.toggleChecklistItemStatus(item.id).subscribe({
        next: () => {
          this.loadTimelineLogs();
        },
        error: (err: any) => {
          this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật mục kiểm tra.', 'Lỗi');
          item.completed = !item.completed;
          item.isCompleted = item.completed;
          this.cdr.detectChanges();
        }
      });
    } else if (item.id && typeof taskSvc.updateChecklistItem === 'function') {
      taskSvc.updateChecklistItem(item.id, { title: item.title, isCompleted: item.completed }).subscribe({
        next: () => {
          this.loadTimelineLogs();
        },
        error: (err: any) => {
          this.toaster.error(err.error?.error?.message || 'Lỗi cập nhật mục kiểm tra.', 'Lỗi');
          item.completed = !item.completed;
          item.isCompleted = item.completed;
          this.cdr.detectChanges();
        }
      });
    }
    this.cdr.detectChanges();
  }

  deleteChecklistItem(index: number, checklistId?: string): void {
    if (!checklistId) {
      this.toaster.error('Không tìm thấy mã định danh (ID) của mục kiểm tra!', 'Lỗi');
      return;
    }
    if (!confirm('Bạn có chắc chắn muốn xóa mục kiểm tra này không?')) return;

    const taskSvc: any = this.taskService;
    if (typeof taskSvc.deleteChecklistItem === 'function') {
      this.isActionLoading = true;
      taskSvc.deleteChecklistItem(checklistId).subscribe({
        next: () => {
          this.isActionLoading = false;
          this.toaster.success('Đã xóa mục kiểm tra.', 'Thông báo');
          this.loadTaskDetail(true);
          this.loadTimelineLogs();
        },
        error: (err: any) => {
          this.isActionLoading = false;
          this.toaster.error(err.error?.error?.message || 'Lỗi khi xóa mục kiểm tra.', 'Lỗi');
          this.cdr.detectChanges();
        }
      });
    } else {
      this.toaster.error('Hệ thống chưa hỗ trợ phương thức xóa mục kiểm tra.', 'Lỗi');
    }
  }

  goBack(): void {
    this.router.navigate(['/tasks/list']);
  }

  loadTaskDetail(isSilent: boolean = false): void {
    if (!isSilent) this.isLoading = true;
    this.taskService.getTaskDetail(this.taskId).subscribe({
      next: (data: LocalTaskDetailDto & { comments?: TaskCommentDto[]; taskComments?: TaskCommentDto[]; histories?: any[]; concurrencyStamp?: string }) => {
        this.taskDetail = data as LocalTaskDetailDto;
        
        const loadedComments = data.comments || data.taskComments || [];
        this.comments = loadedComments;
        
        if (data.subTasks && data.subTasks.length > 0) {
          this.subTaskList = data.subTasks.map((x: any) => ({
            id: x.id || '',
            title: x.title,
            isCompleted: x.isCompleted ?? x.completed ?? false,
            completed: x.isCompleted ?? x.completed ?? false
          }));
        } else {
          this.subTaskList = [];
        }

        const rawChecklists = (data as any).checklistItems || data.checklists || [];
        if (rawChecklists && rawChecklists.length > 0) {
          this.checklistItems = rawChecklists.map((x: any) => ({
            id: x.id || '',
            title: x.title,
            isCompleted: x.isDone ?? x.isCompleted ?? x.completed ?? false,
            completed: x.isDone ?? x.isCompleted ?? x.completed ?? false
          }));
        } else {
          this.checklistItems = [];
        }

        if (this.taskDetail) {
          this.taskDetail.comments = [...this.comments];
          this.taskDetail.histories = data.histories || [];
          this.taskDetail.concurrencyStamp = data.concurrencyStamp;
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
    
    // Nếu bạn muốn dùng Localization Service với key chuẩn của ABP:
    let key = '';
    switch (st) {
      case TaskStatus.New: key = 'Enum:TaskStatus.0'; break;
      case TaskStatus.InProgress: key = 'Enum:TaskStatus.1'; break;
      case TaskStatus.Completed: key = 'Enum:TaskStatus.2'; break;
      case TaskStatus.Cancelled: key = 'Enum:TaskStatus.3'; break;
      default: return String(status || 'N/A');
    }
    
    const translated = this.localizationService.instant(key);
    if (translated.startsWith('Enum:')) {
      const currentLang = this.localizationService.currentLang;
      if (currentLang === 'vi') {
        switch (st) {
          case TaskStatus.New: return 'Mới';
          case TaskStatus.InProgress: return 'Đang thực hiện';
          case TaskStatus.Completed: return 'Hoàn thành';
          case TaskStatus.Cancelled: return 'Đã hủy';
        }
      } else {
        switch (st) {
          case TaskStatus.New: return 'New';
          case TaskStatus.InProgress: return 'In Progress';
          case TaskStatus.Completed: return 'Completed';
          case TaskStatus.Cancelled: return 'Cancelled';
        }
      }
    }
    
    return translated;
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