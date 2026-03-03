import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Clock from "@/assets/icons/clock.svg?react";
import Gamepad from "@/assets/icons/gamepad.svg?react";
import MapPin from "@/assets/icons/map-pin.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { GameNight } from "@/models";

interface Props {
	gameNight: GameNight;
	timeFormat: string;
	dateFormat: string;
}

export const RsvpEventDetails = ({ gameNight, timeFormat, dateFormat }: Props) => {
	const { t } = useTranslation();

	return (
		<BgtCard className="gap-3">
			<BgtText size="6" weight="bold" className="text-center">
				{gameNight.title}
			</BgtText>

			<div className="grid grid-cols-1 md:grid-cols-2 gap-3">
				<div className="flex items-start gap-3 p-3 bg-white/5 rounded-lg">
					<Calendar className="size-5 text-primary shrink-0 mt-0.5" />
					<div>
						<BgtText size="1" color="gray">
							{t("rsvp.when")}
						</BgtText>
						<BgtText size="3" weight="medium" color="white">
							{format(gameNight.startDate, dateFormat)}
						</BgtText>
						<BgtText size="2" color="gray" className="flex items-center gap-1">
							<Clock className="size-4" />
							{format(gameNight.startDate, timeFormat)}
						</BgtText>
					</div>
				</div>

				<div className="flex items-start gap-3 p-3 bg-white/5 rounded-lg">
					<MapPin className="size-5 text-primary shrink-0 mt-0.5" />
					<div>
						<BgtText size="1" color="gray">
							{t("rsvp.location")}
						</BgtText>
						<BgtText size="3" weight="medium" color="white">
							{gameNight.location.name}
						</BgtText>
					</div>
				</div>

				<div className="flex items-start gap-3 p-3 bg-white/5 rounded-lg md:col-span-2">
					<Users className="size-5 text-primary shrink-0 mt-0.5" />
					<div>
						<BgtText size="1" color="gray">
							{t("rsvp.hosted-by")}
						</BgtText>
						<div className="flex items-center gap-2 mt-1">
							<BgtAvatar image={gameNight.host.image} title={gameNight.host.name} withTitle />
						</div>
					</div>
				</div>
			</div>

			{gameNight.suggestedGames.length > 0 && (
				<>
					<BgtText size="2" color="gray" className="flex items-center gap-3">
						<Gamepad className="size-5 text-primary" />
						{t("rsvp.suggested-games")}
					</BgtText>
					<div className="flex flex-wrap gap-2">
						{gameNight.suggestedGames.map((game) => (
							<div
								key={game.id}
								className="pl-1 pr-2 py-1 bg-primary/20 text-primary border border-primary/30 rounded text-sm"
							>
								<BgtAvatar image={game.image} title={game.title} withTitle />
							</div>
						))}
					</div>
				</>
			)}

			{gameNight.notes && (
				<div className="bg-white/5 border border-white/10 rounded-lg p-3">
					<BgtText size="2" color="gray">
						{gameNight.notes}
					</BgtText>
				</div>
			)}
		</BgtCard>
	);
};
