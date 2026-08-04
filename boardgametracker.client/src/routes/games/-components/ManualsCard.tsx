import { type ChangeEvent, useRef } from "react";
import { useTranslation } from "react-i18next";
import Download from "@/assets/icons/download.svg?react";
import List from "@/assets/icons/list.svg?react";
import Plus from "@/assets/icons/plus-circle.svg?react";
import Trash from "@/assets/icons/trash.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";
import { toDisplay } from "@/utils/dateUtils";
import { formatFileSize } from "@/utils/numberUtils";
import { useGameManuals } from "../-hooks/useGameManuals";

interface Props {
	gameId: number;
	canWrite: boolean;
	dateFormat: string;
	uiLanguage: string;
}

export const ManualsCard = ({ gameId, canWrite, dateFormat, uiLanguage }: Props) => {
	const { t } = useTranslation("game");
	const { manuals = [], uploadManuals, deleteManual, downloadManual } = useGameManuals(gameId);
	const fileInputRef = useRef<HTMLInputElement>(null);

	const openFilePicker = () => fileInputRef.current?.click();

	const onFilesSelected = (event: ChangeEvent<HTMLInputElement>) => {
		const files = event.target.files;
		if (files && files.length > 0) {
			uploadManuals(Array.from(files));
		}
		event.target.value = "";
	};

	return (
		<BgtCard
			title={`${t("manuals.title")} (${manuals.length})`}
			icon={List}
			actions={
				canWrite
					? [
							{
								variant: "primary",
								content: <Plus className="size-5" />,
								onClick: openFilePicker,
							},
						]
					: []
			}
		>
			<input
				ref={fileInputRef}
				type="file"
				accept="application/pdf,.pdf"
				multiple
				className="hidden"
				onChange={onFilesSelected}
			/>
			{manuals.length === 0 ? (
				<div className="text-center py-8">
					<div className="text-white/50 text-sm">{t("manuals.none")}</div>
					{canWrite && (
						<button
							type="button"
							onClick={openFilePicker}
							className="mt-3 text-primary hover:text-primary/80 text-sm transition-colors"
						>
							{t("manuals.add")}
						</button>
					)}
				</div>
			) : (
				<div className="space-y-2">
					{manuals.map((manual) => (
						<div
							key={manual.id}
							className="flex items-center gap-3 bg-primary/5 rounded-lg p-4 border border-primary/10 group"
						>
							<div className="shrink-0 w-10 h-10 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
								<List className="text-primary" />
							</div>
							<div className="flex-1 min-w-0">
								<BgtText color="white" className="truncate">
									{manual.title}
								</BgtText>
								<BgtText size="1" color="gray">
									{formatFileSize(manual.fileSizeBytes)} · {toDisplay(manual.uploadDate, dateFormat, uiLanguage)}
								</BgtText>
							</div>
							<div className="flex gap-1">
								<BgtIconButton
									icon={<Download />}
									intent="primary"
									onClick={() => downloadManual(manual.id, manual.title)}
								/>
								{canWrite && <BgtIconButton icon={<Trash />} intent="danger" onClick={() => deleteManual(manual.id)} />}
							</div>
						</div>
					))}
				</div>
			)}
		</BgtCard>
	);
};
