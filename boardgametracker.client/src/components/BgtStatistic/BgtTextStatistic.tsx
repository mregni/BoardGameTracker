import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { type ComponentProps, cloneElement, isValidElement } from "react";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import Plus from "@/assets/icons/plus.svg?react";
import { formatNumber } from "@/utils/numberUtils";
import { BgtCard } from "../BgtCard/BgtCard";
import { BgtText } from "../BgtText/BgtText";

interface Props {
	title: string;
	content: string | number | null;
	suffix?: string | number | null;
	icon?: React.ReactNode;
	iconClassName?: string;
	textSize?: "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";
	link?: string;
	addLink?: string;
	addLabel?: string;
	onClick?: () => void;
	action?: React.ReactNode;
	caption?: React.ReactNode;
	valueColor?: ComponentProps<typeof BgtText>["color"];
}

const formatContent = (content: string | number, language: string | undefined): string =>
	typeof content === "number" ? formatNumber(content, language) : content;

export const BgtTextStatistic = (props: Props) => {
	const {
		title,
		content,
		suffix,
		icon,
		iconClassName,
		textSize = "5",
		link,
		addLink,
		addLabel,
		onClick,
		action,
		caption,
		valueColor = "cyan",
	} = props;
	const { i18n } = useTranslation();

	if (content === null || content === undefined) return null;

	const isInteractive = Boolean(link || onClick);

	const iconWithClasses =
		icon && isValidElement(icon)
			? cloneElement(icon as React.ReactElement<{ className?: string }>, {
					className: cx("size-5", (icon.props as { className?: string }).className, iconClassName),
				})
			: icon;

	const card = (
		<BgtCard
			className={cx(
				"col-span-1 h-full",
				isInteractive && "border-primary/25 transition-colors hover:border-primary/60 cursor-pointer",
			)}
		>
			<div className="flex min-h-6 items-center gap-2 text-stat-title mb-2">
				{iconWithClasses}
				<span className="line-clamp-2 break-words" title={title}>
					{title}
				</span>
				{action ? (
					<span className="ml-auto flex shrink-0 items-center">{action}</span>
				) : (
					isInteractive && <CaretRight className="size-4 ml-auto shrink-0 text-primary" />
				)}
			</div>
			<BgtText size={textSize} color={valueColor} weight="bold">
				{formatContent(content, i18n.language)}
				{suffix === "%" && "%"}
				{suffix && suffix !== "%" && <span className="text-sm lowercase">&nbsp;{suffix}</span>}
			</BgtText>
			{caption && (
				<span
					className="mt-1 block text-xs break-words text-white/50"
					title={typeof caption === "string" ? caption : undefined}
				>
					{caption}
				</span>
			)}
		</BgtCard>
	);

	if (addLink) {
		return (
			<div className="relative col-span-1 h-full">
				{link ? (
					<Link to={link} className="block h-full">
						{card}
					</Link>
				) : (
					card
				)}
				<Link
					to={addLink}
					aria-label={addLabel}
					className="absolute bottom-3 right-3 z-10 inline-flex items-center justify-center size-8 rounded-lg bg-primary/80 text-white transition-colors hover:bg-primary"
				>
					<Plus className="size-4" />
				</Link>
			</div>
		);
	}

	if (link) {
		return (
			<Link to={link} className="col-span-1">
				{card}
			</Link>
		);
	}

	if (onClick) {
		return (
			<button type="button" onClick={onClick} className="col-span-1 h-full text-left">
				{card}
			</button>
		);
	}

	return card;
};
