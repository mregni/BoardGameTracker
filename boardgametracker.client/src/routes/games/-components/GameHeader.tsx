import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import type { GameState } from "@/models";
import { getColorFromGameState, getItemStateTranslationKey } from "@/utils/ItemStateUtils";

interface Props {
	gameTitle: string;
	gameState: GameState;
	isLoaned: boolean;
	onAddSession: () => void;
	onEdit: () => void;
	onDelete: () => void;
}

export const GameHeader = (props: Props) => {
	const { gameTitle, gameState, isLoaned, onAddSession, onEdit, onDelete } = props;
	const { t } = useTranslation();

	return (
		<div className="flex md:flex-row flex-col justify-between">
			<div className="flex flex-col-reverse xl:flex-row xl:gap-3 gap-1">
				<BgtHeading size="8" className="shrink-0">
					{gameTitle}
				</BgtHeading>
				<BgtText
					size="2"
					className="line-clamp-1 uppercase"
					weight="medium"
					color={getColorFromGameState(gameState, isLoaned)}
				>
					{t(getItemStateTranslationKey(gameState, isLoaned))}
				</BgtText>
			</div>
			<div className="flex gap-3 justify-between md:justify-start items-center pt-2 md:pt-0">
				<BgtButton variant="primary" onClick={onAddSession}>
					{t("game.add")}
				</BgtButton>
				<BgtEditDeleteButtons onDelete={onDelete} onEdit={onEdit} />
			</div>
		</div>
	);
};
