import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";

import type { MenuItem } from "@/models";

interface Props extends React.HTMLAttributes<HTMLDivElement> {
	item: MenuItem;
	count: number | undefined;
	onClick?: () => void;
}

export const BgtMenuItem = (props: Props) => {
	const { item, count, onClick, className } = props;
	const { t } = useTranslation();

	const Icon = item.icon;

	return (
		<Link
			onClick={onClick}
			to={item.path}
			activeProps={{ className: "bg-primary/60 border border-primary" }}
			className={cx(
				"flex w-full p-2 text-white cursor-pointer border border-transparent items-center my-1 hover:bg-primary/60 md:rounded-lg font-semibold justify-between px-2",
				className,
			)}
		>
			<div className="flex items-center gap-2">
				<Icon className="size-5" />
				<span className="text-[16px]/[24px]">{t(item.menuLabel)}</span>
			</div>
			{count !== undefined && (
				<div
					className="py-1 px-3 flex items-center justify-center text-xs"
					title={item.countLabel ? t(item.countLabel, { count }) : undefined}
				>
					{count}
				</div>
			)}
		</Link>
	);
};
