import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import LeftRightArrow from "@/assets/icons/left-right-arrow.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import type { Game } from "@/models";

interface Props {
	games: Pick<Game, "id" | "title" | "image">[];
}

export const PlayerLoansCard = ({ games }: Props) => {
	const { t } = useTranslation("player");

	if (games.length === 0) {
		return null;
	}

	return (
		<BgtCard title={t("cards.games-on-loan")} icon={LeftRightArrow}>
			<ul className="-mt-2 flex flex-col divide-y divide-white/10">
				{games.map((game) => (
					<li key={game.id}>
						<Link
							to="/games/$gameId"
							params={{ gameId: game.id }}
							className="-mx-2 flex items-center gap-3 rounded-md px-2 py-2 transition-colors hover:bg-primary/10"
						>
							<span aria-hidden="true">
								<BgtAvatar image={game.image} title={game.title} size="large" />
							</span>
							<span className="min-w-0 flex-1 truncate font-semibold">{game.title}</span>
						</Link>
					</li>
				))}
			</ul>
		</BgtCard>
	);
};
