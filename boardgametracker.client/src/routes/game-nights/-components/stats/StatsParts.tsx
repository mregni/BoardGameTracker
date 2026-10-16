import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import type { Game, Location, Player } from "@/models";

export interface StatsLookup {
	players: Map<number, Player>;
	games: Map<number, Game>;
	locations: Map<number, Location>;
	uiLanguage: string;
	dateFormat: string;
	timeFormat: string;
}

interface CardProps {
	id: string;
	title: string;
	subtitle?: string;
	action?: ReactNode;
	children: ReactNode;
}

export const StatsCard = ({ id, title, subtitle, action, children }: CardProps) => (
	<section aria-labelledby={id} className="min-w-0">
		<BgtCard className="@container gap-2">
			<div className="flex flex-wrap items-baseline justify-between gap-2">
				<h2 id={id} className="font-semibold text-white">
					{title}
					{subtitle && <span className="ml-2 text-xs font-normal text-gray-400">{subtitle}</span>}
				</h2>
				{action}
			</div>
			{children}
		</BgtCard>
	</section>
);

interface RowProps {
	rank?: number | null;
	highlight?: boolean;
	avatar: ReactNode;
	name: string;
	detail?: ReactNode;
	bar: ReactNode;
	value: ReactNode;
	title?: string;
	pressed?: boolean;
	onClick?: () => void;
	label?: string;
}

const rowClasses =
	"grid w-full grid-cols-[1.25rem_1.75rem_minmax(0,1fr)_auto] items-center gap-x-2.5 gap-y-1 rounded-lg px-1 py-1.5 text-left @md:grid-cols-[1.25rem_1.75rem_minmax(0,1fr)_minmax(4.5rem,1.1fr)_4.5rem] @md:px-2";

export const StatRow = (props: RowProps) => {
	const { rank, highlight, avatar, name, detail, bar, value, title, pressed, onClick, label } = props;

	const cells = (
		<>
			<span
				className={cx(
					"row-span-2 text-right text-xs tabular-nums @md:row-span-1",
					highlight ? "text-gold" : "text-gray-400",
				)}
			>
				{rank}
			</span>
			<span className="row-span-2 flex @md:row-span-1">{avatar}</span>
			<span className="min-w-0">
				<strong className="block truncate text-sm text-white">{name}</strong>
				{detail && <span className="block text-[11px] leading-tight text-gray-400">{detail}</span>}
			</span>
			<span className="text-right font-bold tabular-nums text-white @md:col-start-5 @md:row-start-1">{value}</span>
			<span className="col-span-2 col-start-3 @md:col-span-1 @md:col-start-4 @md:row-start-1">{bar}</span>
		</>
	);

	if (onClick) {
		return (
			<button
				type="button"
				title={title}
				aria-label={label}
				aria-pressed={pressed}
				onClick={onClick}
				className={cx(
					rowClasses,
					"cursor-pointer border hover:bg-card-light",
					pressed ? "border-primary/40 bg-card-light" : "border-transparent",
				)}
			>
				{cells}
			</button>
		);
	}

	return (
		<div title={title} className={cx(rowClasses, "hover:bg-card-light")}>
			{cells}
		</div>
	);
};

interface FactProps {
	icon: ReactNode;
	title: ReactNode;
	value?: ReactNode;
	hint?: ReactNode;
	tone?: "gold" | "primary";
}

export const StatFact = ({ icon, title, value, hint, tone = "primary" }: FactProps) => (
	<div className="grid grid-cols-[2.125rem_minmax(0,1fr)] items-center gap-2.5 rounded-[10px] bg-card-light p-2.5">
		<span
			aria-hidden="true"
			className={cx(
				"grid size-[2.125rem] place-items-center rounded-lg [&>svg]:size-4",
				tone === "gold" ? "bg-gold/10 text-gold" : "bg-primary/15 text-indigo-300",
			)}
		>
			{icon}
		</span>
		<div className="min-w-0">
			<strong className="block text-sm text-white">
				{title}
				{value !== undefined && <span className="ml-1.5 font-bold tabular-nums text-card-value">{value}</span>}
			</strong>
			{hint && <span className="block text-xs text-gray-400">{hint}</span>}
		</div>
	</div>
);

export const StatsEmpty = ({ children }: { children: ReactNode }) => (
	<p className="text-sm text-gray-400">{children}</p>
);
