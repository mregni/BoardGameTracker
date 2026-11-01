import { cx } from "class-variance-authority";
import { Fragment, type KeyboardEvent, useEffect, useId, useRef, useState } from "react";
import Dots from "@/assets/icons/dots-horizontal.svg?react";

export interface BgtActionMenuItem {
	label: string;
	onSelect: () => void;
	destructive?: boolean;
}

interface Props {
	label: string;
	items: BgtActionMenuItem[];
}

export const BgtActionMenu = ({ label, items }: Props) => {
	const [focusIndex, setFocusIndex] = useState<number | null>(null);
	const open = focusIndex !== null;
	const rootRef = useRef<HTMLDivElement>(null);
	const triggerRef = useRef<HTMLButtonElement>(null);
	const itemRefs = useRef<(HTMLButtonElement | null)[]>([]);
	const menuId = useId();
	const firstDestructive = items.findIndex((item) => item.destructive);

	useEffect(() => {
		if (focusIndex !== null) itemRefs.current[focusIndex]?.focus();
	}, [focusIndex]);

	useEffect(() => {
		if (!open) return;

		const handlePointer = (event: PointerEvent) => {
			if (!rootRef.current?.contains(event.target as Node)) setFocusIndex(null);
		};

		document.addEventListener("pointerdown", handlePointer);
		return () => document.removeEventListener("pointerdown", handlePointer);
	}, [open]);

	const close = (returnFocus: boolean) => {
		setFocusIndex(null);
		if (returnFocus) triggerRef.current?.focus();
	};

	const handleTriggerKey = (event: KeyboardEvent<HTMLButtonElement>) => {
		if (event.key === "ArrowDown" || event.key === "ArrowUp") {
			event.preventDefault();
			setFocusIndex(event.key === "ArrowDown" ? 0 : items.length - 1);
		}
	};

	const handleMenuKey = (event: KeyboardEvent<HTMLDivElement>) => {
		if (focusIndex === null) return;
		const last = items.length - 1;
		const next: Record<string, number> = {
			ArrowDown: focusIndex === last ? 0 : focusIndex + 1,
			ArrowUp: focusIndex === 0 ? last : focusIndex - 1,
			Home: 0,
			End: last,
		};

		if (event.key in next) {
			event.preventDefault();
			setFocusIndex(next[event.key]);
		} else if (event.key === "Escape") {
			event.preventDefault();
			close(true);
		} else if (event.key === "Tab") {
			close(false);
		}
	};

	return (
		<div
			ref={rootRef}
			className="relative"
			onBlur={(event) => {
				if (open && !rootRef.current?.contains(event.relatedTarget as Node | null)) setFocusIndex(null);
			}}
		>
			<button
				ref={triggerRef}
				type="button"
				aria-label={label}
				aria-haspopup="menu"
				aria-expanded={open}
				aria-controls={open ? menuId : undefined}
				onClick={() => (open ? close(false) : setFocusIndex(0))}
				onKeyDown={handleTriggerKey}
				className="flex size-11 items-center justify-center rounded-md border border-white/10 text-white/70 hover:bg-white/10 hover:text-white md:size-9"
			>
				<Dots className="size-5" aria-hidden="true" />
			</button>
			{open && (
				<div
					id={menuId}
					role="menu"
					aria-label={label}
					onKeyDown={handleMenuKey}
					className="absolute right-0 z-30 mt-1 min-w-44 overflow-hidden rounded-lg border border-white/10 bg-background py-1 shadow-2xl"
				>
					{items.map((item, index) => (
						<Fragment key={item.label}>
							{index === firstDestructive && index > 0 && <hr className="my-1 border-white/10" />}
							<button
								ref={(element) => {
									itemRefs.current[index] = element;
								}}
								type="button"
								role="menuitem"
								tabIndex={-1}
								onClick={() => {
									close(true);
									item.onSelect();
								}}
								className={cx(
									"block w-full px-3 py-2.5 text-left text-sm hover:bg-white/10 focus-visible:bg-white/10 focus-visible:outline-none",
									item.destructive ? "text-bad" : "text-white",
								)}
							>
								{item.label}
							</button>
						</Fragment>
					))}
				</div>
			)}
		</div>
	);
};
