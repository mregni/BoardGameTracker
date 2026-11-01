import { useNavigate } from "@tanstack/react-router";
import { type JSX, memo, type ReactNode, useCallback } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "./BgtCard";

interface RecentActivityCardProps<T> {
	items: T[];
	renderItem: (item: T) => ReactNode;
	title: string;
	viewAllRoute: string;
	viewAllText?: string;
	icon?: React.ComponentType<React.SVGProps<SVGSVGElement>>;
	getKey: (item: T) => string | number;
}

const RecentActivityCardComponent = <T,>({
	items,
	renderItem,
	title,
	viewAllRoute,
	viewAllText,
	icon,
	getKey,
}: RecentActivityCardProps<T>) => {
	const { t } = useTranslation("game");
	const navigate = useNavigate();

	const handleViewAll = useCallback(() => {
		navigate({ to: viewAllRoute });
	}, [navigate, viewAllRoute]);

	return (
		<BgtCard title={title} icon={icon}>
			<ul className="-mt-2 flex flex-col divide-y divide-white/10">
				{items.map((item) => (
					<li key={getKey(item)}>{renderItem(item)}</li>
				))}
			</ul>
			<BgtButton variant="primary" onClick={handleViewAll} className="mt-3">
				{viewAllText ?? t("sessions")}
			</BgtButton>
		</BgtCard>
	);
};

RecentActivityCardComponent.displayName = "RecentActivityCard";

export const RecentActivityCard = memo(RecentActivityCardComponent) as <T>(
	props: RecentActivityCardProps<T>,
) => JSX.Element;
