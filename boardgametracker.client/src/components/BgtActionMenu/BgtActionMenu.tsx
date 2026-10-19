import { cx } from "class-variance-authority";
import { useEffect, useId, useRef, useState } from "react";
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
	const [open, setOpen] = useState(false);
	const rootRef = useRef<HTMLDivElement>(null);
	const triggerRef = useRef<HTMLButtonElement>(null);
	const menuId = useId();

	useEffect(() => {
		if (!open) return;

		const handlePointer = (event: PointerEvent) => {
			if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
		};
		const handleKey = (event: KeyboardEvent) => {
			if (event.key === "Escape") {
				setOpen(false);
				triggerRef.current?.focus();
			}
		};

		document.addEventListener("pointerdown", handlePointer);
		document.addEventListener("keydown", handleKey);
		return () => {
			document.removeEventListener("pointerdown", handlePointer);
			document.removeEventListener("keydown", handleKey);
		};
	}, [open]);

	return (
		<div ref={rootRef} className="relative">
			<button
				ref={triggerRef}
				type="button"
				aria-label={label}
				aria-haspopup="menu"
				aria-expanded={open}
				aria-controls={open ? menuId : undefined}
				onClick={() => setOpen((current) => !current)}
				className="flex size-9 items-center justify-center rounded-md border border-white/10 text-white/70 hover:bg-white/10 hover:text-white"
			>
				<Dots className="size-5" aria-hidden="true" />
			</button>
			{open && (
				<div
					id={menuId}
					role="menu"
					aria-label={label}
					className="absolute right-0 z-30 mt-1 min-w-44 overflow-hidden rounded-lg border border-white/10 bg-background py-1 shadow-2xl"
				>
					{items.map((item) => (
						<button
							key={item.label}
							type="button"
							role="menuitem"
							onClick={() => {
								setOpen(false);
								item.onSelect();
							}}
							className={cx(
								"block w-full px-3 py-2.5 text-left text-sm hover:bg-white/10",
								item.destructive ? "text-bad" : "text-white",
							)}
						>
							{item.label}
						</button>
					))}
				</div>
			)}
		</div>
	);
};
