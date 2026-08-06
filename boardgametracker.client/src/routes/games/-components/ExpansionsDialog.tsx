import { useTranslation } from "react-i18next";
import Package from "@/assets/icons/package.svg?react";
import Trash from "@/assets/icons/trash.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogContent, BgtDialogDescription, BgtDialogTitle } from "@/components/BgtDialog/BgtDialog";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";

interface Expansion {
	id: number;
	title: string;
}

interface Props {
	expansions: Expansion[];
	open: boolean;
	close: () => void;
	canWrite: boolean;
	onAddExpansion: () => void;
	onDeleteExpansion: (expansionId: number) => void;
}

export const ExpansionsDialog = (props: Props) => {
	const { expansions, open, close, canWrite, onAddExpansion, onDeleteExpansion } = props;
	const { t } = useTranslation("game");

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent className="max-w-lg">
				<BgtDialogTitle className="flex items-center gap-2">
					<Package className="size-5 text-primary" />
					{`${t("expansions.title")} (${expansions.length})`}
				</BgtDialogTitle>
				<BgtDialogDescription>{t("expansions.description")}</BgtDialogDescription>
				{expansions.length === 0 ? (
					<div className="text-center py-8">
						<div className="text-white/50 text-sm">{t("expansions.none")}</div>
					</div>
				) : (
					<div className="space-y-2 py-2 max-h-[60vh] overflow-y-auto">
						{expansions.map((expansion) => (
							<div
								key={expansion.id}
								className="flex items-center gap-3 bg-primary/5 rounded-lg p-4 border border-primary/10 group"
							>
								<div className="shrink-0 w-10 h-10 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
									<Package className="text-primary" />
								</div>
								<div className="flex-1">
									<BgtText color="white">{expansion.title}</BgtText>
								</div>
								{canWrite && (
									<div className="flex">
										<BgtIconButton icon={<Trash />} intent="danger" onClick={() => onDeleteExpansion(expansion.id)} />
									</div>
								)}
							</div>
						))}
					</div>
				)}
				{canWrite && (
					<div className="flex justify-end pt-2">
						<BgtButton onClick={onAddExpansion}>{t("expansions.add")}</BgtButton>
					</div>
				)}
			</BgtDialogContent>
		</BgtDialog>
	);
};
