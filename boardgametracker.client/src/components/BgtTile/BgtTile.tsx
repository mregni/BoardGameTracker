import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";
import CaretRight from "@/assets/icons/caret-right.svg?react";

interface Props {
	title: string;
	link: string;
	visual: ReactNode;
	stats: ReactNode;
}

export const BgtTile = (props: Props) => {
	const { title, link, visual, stats } = props;

	return (
		<Link
			to={link}
			className="grid grid-cols-[2.75rem_minmax(0,1fr)_auto] items-center gap-3 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2.5 sm:p-3 cursor-pointer transition-colors hover:border-primary/60 focus-visible:border-primary/60"
		>
			<div className="flex size-11 items-center justify-center">{visual}</div>
			<div className="min-w-0">
				<div className="truncate font-medium text-white" title={title}>
					{title}
				</div>
				<div className="text-xs text-white/60 tabular-nums">{stats}</div>
			</div>
			<CaretRight aria-hidden="true" className="size-4 shrink-0 text-white/50" />
		</Link>
	);
};
