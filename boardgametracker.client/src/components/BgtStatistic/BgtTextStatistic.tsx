import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { cloneElement, isValidElement } from "react";
import Plus from "@/assets/icons/plus.svg?react";
import { BgtCard } from "../BgtCard/BgtCard";
import { BgtText } from "../BgtText/BgtText";

interface Props {
	title: string;
	content: string | number | null;
	suffix?: string | number | null;
	prefix?: string | number | null;
	icon?: React.ReactNode;
	iconClassName?: string;
	textSize?: "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";
	link?: string;
	addLink?: string;
	addLabel?: string;
}

export const BgtTextStatistic = (props: Props) => {
	const { title, content, suffix, prefix, icon, iconClassName, textSize = "5", link, addLink, addLabel } = props;

	if (content === null || content === undefined) return null;

	const iconWithClasses =
		icon && isValidElement(icon)
			? cloneElement(icon as React.ReactElement<{ className?: string }>, {
					className: cx("size-5", (icon.props as { className?: string }).className, iconClassName),
				})
			: icon;

	const card = (
		<BgtCard className={cx("col-span-1 h-full", link && "transition-colors hover:border-primary/50 cursor-pointer")}>
			<div className="flex items-center gap-2 text-primary/70 mb-2">
				{iconWithClasses}
				<span>{title}</span>
			</div>
			<BgtText size={textSize} color="cyan" weight="bold">
				{prefix && <span>{prefix}&nbsp;</span>}
				{content.toLocaleString()}
				{suffix && <span className="text-sm lowercase">&nbsp;{suffix}</span>}
			</BgtText>
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

	return card;
};
