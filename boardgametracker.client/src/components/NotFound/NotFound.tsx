import { Link, useRouter } from "@tanstack/react-router";
import type { ComponentType, SVGProps } from "react";
import { useTranslation } from "react-i18next";
import ArrowLeft from "@/assets/icons/arrow-left.svg?react";
import CalendarIcon from "@/assets/icons/calendar.svg?react";
import Game from "@/assets/icons/gamepad.svg?react";
import HomeIcon from "@/assets/icons/home.svg?react";
import MagnifyingGlass from "@/assets/icons/magnifying-glass.svg?react";
import UsersIcon from "@/assets/icons/users.svg?react";
import BgtButton from "../BgtButton/BgtButton";
import { BgtText } from "../BgtText/BgtText";

interface NavCard {
	to: string;
	icon: ComponentType<SVGProps<SVGSVGElement>>;
	titleKey: string;
	descriptionKey: string;
}

const navCards: NavCard[] = [
	{
		to: "/",
		icon: HomeIcon,
		titleKey: "common.dashboard",
		descriptionKey: "not-found.nav.dashboard-description",
	},
	{
		to: "/games",
		icon: Game,
		titleKey: "common.games",
		descriptionKey: "not-found.nav.games-description",
	},
	{
		to: "/players",
		icon: UsersIcon,
		titleKey: "common.players",
		descriptionKey: "not-found.nav.players-description",
	},
	{
		to: "/game-nights",
		icon: CalendarIcon,
		titleKey: "common.game-nights",
		descriptionKey: "not-found.nav.game-nights-description",
	},
];

export const NotFound = () => {
	const { t } = useTranslation();
	const router = useRouter();

	return (
		<div className="flex flex-col items-center justify-center min-h-screen p-4 gap-6">
			<div className="flex flex-col items-center text-center gap-4">
				<span className="text-8xl md:text-9xl font-black text-primary/60 leading-none">404</span>
				<BgtText size="6" weight="bold" color="white">
					{t("not-found.title")}
				</BgtText>
				<BgtText color="white" opacity={70}>
					{t("not-found.subtitle")}
				</BgtText>
				<BgtText size="2" color="white" opacity={50}>
					{t("not-found.description")}
				</BgtText>
			</div>

			<BgtButton variant="primary" size="3" onClick={() => router.history.back()}>
				<ArrowLeft className="size-4" />
				{t("not-found.go-back")}
			</BgtButton>

			<div className="bg-primary/10 border border-primary/20 rounded-lg p-6 w-full max-w-lg mt-4">
				<div className="flex items-center justify-center gap-2 mb-4">
					<MagnifyingGlass className="size-5 text-primary" />
					<BgtText weight="bold" color="white">
						{t("not-found.quick-nav")}
					</BgtText>
				</div>
				<div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
					{navCards.map((card) => (
						<Link
							key={card.to}
							to={card.to}
							className="flex items-center gap-3 p-3 rounded-lg border border-primary/20 bg-primary/5 hover:bg-primary/15 transition-colors"
						>
							<div className="bg-primary/20 rounded-lg p-2">
								<card.icon className="size-5 text-primary" />
							</div>
							<div>
								<BgtText size="2" weight="bold" color="white">
									{t(card.titleKey)}
								</BgtText>
								<BgtText size="1" color="white" opacity={50}>
									{t(card.descriptionKey)}
								</BgtText>
							</div>
						</Link>
					))}
				</div>
			</div>
		</div>
	);
};
