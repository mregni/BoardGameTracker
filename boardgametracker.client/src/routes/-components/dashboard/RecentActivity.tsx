import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { RecentActivity } from "@/models";
import { toRelative } from "@/utils/dateUtils";

interface Props extends React.HTMLAttributes<HTMLDivElement> {
	activities: RecentActivity[];
}

export const RecentActivityCard = (props: Props) => {
	const { activities, className } = props;
	const { t } = useTranslation();

	return (
		<BgtCard title={t("dashboard.recent-activity")} icon={Calendar} className={className}>
			<div className="flex flex-col gap-3">
				{activities.map((activity) => (
					<ActivityItem key={activity.id} activity={activity} />
				))}
			</div>
		</BgtCard>
	);
};

interface ItemProps {
	activity: RecentActivity;
}

const ActivityItem = ({ activity }: ItemProps) => {
	const { t, i18n } = useTranslation();

	return (
		<BgtCard className="cursor-pointer p-3">
			<div className="flex items-center gap-4">
				<Link to="/games/$gameId" params={{ gameId: activity.gameId }} className="w-10 h-10 rounded-full overflow-hidden bg-primary/20 border border-primary/30 shrink-0">
					<BgtAvatar
						image={activity.gameImage}
						title={activity.gameTitle}
						size="large"
					/>
				</Link>
				<div className="flex-1">
					<BgtText color="white">
						{activity.winnerName && (
							<>
								<Link className="font-bold" to="/players/$playerId" params={{ playerId: activity.winnerId }}>
									{activity.winnerName}
								</Link>{" "}
								<span className="lowercase">{t("common.won")}</span>{" "}
							</>
						)}
						<Link className="text-primary" to="/games/$gameId" params={{ gameId: activity.gameId }}>
							{activity.gameTitle}
						</Link>
					</BgtText>
					<BgtText color="white" size="2" opacity={50}>
						{t("common.player", { count: activity.playerCount })} • {activity.durationInMinutes}
						{t("common.minutes-abbreviation")} • {toRelative(activity.start, i18n.language)}
					</BgtText>
				</div>
				<div className="font-bold">
					<Trophy className="size-5 text-yellow-500" />
				</div>
			</div>
		</BgtCard>
	);
};
