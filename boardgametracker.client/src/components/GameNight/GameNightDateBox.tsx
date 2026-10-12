import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { getDateFnsLocale } from "@/utils/localeUtils";

interface Props {
	date: Date | string;
	uiLanguage: string;
	variant?: "highlight" | "soft" | "compact";
	className?: string;
}

export const GameNightDateBox = (props: Props) => {
	const { date, uiLanguage, variant = "soft", className } = props;
	const locale = getDateFnsLocale(uiLanguage);
	const value = new Date(date);

	return (
		<div
			className={cx(
				"flex flex-col items-center text-center leading-tight",
				variant === "highlight" && "rounded-xl bg-primary px-1 py-2 text-white",
				variant === "soft" && "rounded-xl border border-white/10 bg-white/5 px-1 py-2",
				variant === "compact" && "text-xs text-gray-400",
				className,
			)}
		>
			<span className="text-[11px] uppercase tracking-widest opacity-85">{format(value, "EEE", { locale })}</span>
			<span className={cx("font-bold", variant === "compact" ? "text-lg text-white" : "text-2xl md:text-3xl")}>
				{format(value, "d", { locale })}
			</span>
			<span className="text-[11px] uppercase tracking-widest opacity-85">{format(value, "MMM", { locale })}</span>
		</div>
	);
};
