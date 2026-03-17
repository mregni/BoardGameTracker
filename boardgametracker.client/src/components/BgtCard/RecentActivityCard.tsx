import { useNavigate } from "@tanstack/react-router";
import { memo, type ReactNode, useCallback } from "react";
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
			<div className="flex flex-col gap-3">
				{items.map((item) => (
					<div key={getKey(item)}>{renderItem(item)}</div>
				))}
				<BgtButton variant="primary" onClick={handleViewAll} className="flex-1 hidden md:flex">
					{viewAllText ?? t("sessions")}
				</BgtButton>
			</div>
		</BgtCard>
	);
};

RecentActivityCardComponent.displayName = "RecentActivityCard";

export const RecentActivityCard = memo(RecentActivityCardComponent) as <T>(
	props: RecentActivityCardProps<T>,
) => JSX.Element;
