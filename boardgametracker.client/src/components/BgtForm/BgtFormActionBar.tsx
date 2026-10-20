import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { useHideBottomNav } from "@/hooks/useHideBottomNav";

interface Props {
	children: ReactNode;
	className?: string;
}

export const BgtFormActionBar = (props: Props) => {
	const { children, className } = props;
	useHideBottomNav();

	return (
		<>
			<div
				aria-hidden="true"
				data-slot="form-action-bar-spacer"
				className="h-(--form-action-bar-h) shrink-0 md:hidden"
			/>
			<div
				data-slot="form-action-bar"
				className={cx(
					"fixed inset-x-0 bottom-0 z-30 flex h-(--form-action-bar-h) items-center border-t border-white/10 bg-background/95 px-4 pb-[env(safe-area-inset-bottom)] backdrop-blur-sm",
					"md:static md:z-auto md:h-auto md:border-0 md:bg-transparent md:p-0 md:backdrop-blur-none",
					className,
				)}
			>
				<div className="w-full">{children}</div>
			</div>
		</>
	);
};
