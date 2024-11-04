@__const.main.nums = private unnamed_addr constant [10 x i32] [i32 0, i32 1, i32 2, i32 3, i32 4, i32 5, i32 6, i32 7, i32 8, i32 9], align 16

define dso_local noundef i32 @_Z3fooPii(ptr nocapture noundef readonly %0, i32 noundef %1) local_unnamed_addr #0 {
  %3 = icmp sgt i32 %1, 0
  br i1 %3, label %4, label %28

4:
  %5 = zext i32 %1 to i64
  %6 = icmp ult i32 %1, 8
  br i1 %6, label %25, label %7

7:
  %8 = and i64 %5, 4294967288
  br label %9

9:
  %10 = phi i64 [ 0, %7 ], [ %19, %9 ]
  %11 = phi <4 x i32> [ zeroinitializer, %7 ], [ %17, %9 ]
  %12 = phi <4 x i32> [ zeroinitializer, %7 ], [ %18, %9 ]
  %13 = getelementptr inbounds i32, ptr %0, i64 %10
  %14 = load <4 x i32>, ptr %13, align 4
  %15 = getelementptr inbounds i32, ptr %13, i64 4
  %16 = load <4 x i32>, ptr %15, align 4
  %17 = add <4 x i32> %14, %11
  %18 = add <4 x i32> %16, %12
  %19 = add nuw i64 %10, 8
  %20 = icmp eq i64 %19, %8
  br i1 %20, label %21, label %9

21:
  %22 = add <4 x i32> %18, %17
  %23 = tail call i32 @llvm.vector.reduce.add.v4i32(<4 x i32> %22)
  %24 = icmp eq i64 %8, %5
  br i1 %24, label %28, label %25

25:
  %26 = phi i64 [ 0, %4 ], [ %8, %21 ]
  %27 = phi i32 [ 0, %4 ], [ %23, %21 ]
  br label %30

28:
  %29 = phi i32 [ 0, %2 ], [ %23, %21 ], [ %35, %30 ]
  ret i32 %29

30:
  %31 = phi i64 [ %36, %30 ], [ %26, %25 ]
  %32 = phi i32 [ %35, %30 ], [ %27, %25 ]
  %33 = getelementptr inbounds i32, ptr %0, i64 %31
  %34 = load i32, ptr %33, align 4
  %35 = add nsw i32 %34, %32
  %36 = add nuw nsw i64 %31, 1
  %37 = icmp eq i64 %36, %5
  br i1 %37, label %28, label %30
}

define dso_local noundef i32 @main() local_unnamed_addr #2 {
  %1 = tail call noundef i32 @_Z3fooPii(ptr noundef nonnull @__const.main.nums, i32 noundef 10)
  ret i32 %1
}

declare i32 @llvm.vector.reduce.add.v4i32(<4 x i32>) #4

attributes #0 = { mustprogress nofree noinline norecurse nosync nounwind willreturn memory(argmem: read) uwtable "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
attributes #1 = { mustprogress nocallback nofree nosync nounwind speculatable willreturn memory(none) }
attributes #2 = { mustprogress nofree norecurse nosync nounwind willreturn memory(none) uwtable "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
attributes #3 = { nocallback nofree nosync nounwind speculatable willreturn memory(none) }
attributes #4 = { nocallback nofree nosync nounwind willreturn memory(none) }