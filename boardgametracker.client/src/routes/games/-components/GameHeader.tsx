import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameState } from "@/models";
import { getColorFromGameState, getItemStateTranslationKey } from "@/utils/ItemStateUtils";

interface Props {
	gameTitle: string;
	gameState: GameState;
	isLoaned: boolean;
	loan?: { playerName: string; since: string };
	canWrite: boolean;
	showAddSession?: boolean;
	onAddSession: () => void;
	onMarkOwned?: () => void;
	onEdit: () => void;
	onDelete: () => void;
}

export const GameHeader = (props: Props) => {
	const {
		gameTitle,
		gameState,
		isLoaned,
		loan,
		canWrite,
		showAddSession = true,
		onAddSession,
		onMarkOwned,
		onEdit,
		onDelete,
	} = props;
	const { t } = useTranslation("game");
	const offerMarkOwned = gameState === GameState.Wanted && onMarkOwned !== undefined;

	return (
		<div className="flex flex-wrap items-start justify-between gap-x-6 gap-y-3">
			<div className="flex min-w-0 flex-col xl:flex-row xl:items-center xl:gap-3 gap-1">
				<div className="flex items-center gap-2 shrink-0">
					<BgtHeading as="h1" size="8">
						{gameTitle}
					</BgtHeading>
				</div>
				<BgtText size="2" className="line-clamp-1" weight="medium" color={getColorFromGameState(gameState, isLoaned)}>
					{isLoaned && loan ? (
						<Link to="/loans" className="underline-offset-4 hover:underline">
							{t("state.on-loan-to", { name: loan.playerName, date: loan.since })}
						</Link>
					) : (
						t(getItemStateTranslationKey(gameState, isLoaned))
					)}
				</BgtText>
			</div>
			{canWrite && (
				<div className="flex flex-wrap gap-3 items-center max-md:w-full max-md:justify-between">
					{offerMarkOwned && (
						<BgtButton variant="primary" onClick={onMarkOwned} className="whitespace-nowrap">
							{t("mark-owned")}
						</BgtButton>
					)}
					{showAddSession && (
						<BgtButton
							variant={offerMarkOwned ? "secondary" : "primary"}
							onClick={onAddSession}
							className="whitespace-nowrap"
						>
							{t("add")}
						</BgtButton>
					)}
					<BgtEditDeleteButtons onDelete={onDelete} onEdit={onEdit} />
				</div>
			)}
		</div>
	);
};
