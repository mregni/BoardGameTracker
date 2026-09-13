import { useTranslation } from "react-i18next";
import type { RagCitation } from "@/models";

interface Props {
	citations: RagCitation[];
	activeIndex?: number;
	onSelect: (index: number) => void;
}

export const CitationList = ({ citations, activeIndex, onSelect }: Props) => {
	const { t } = useTranslation("chat");

	return (
		<div className="flex flex-col gap-2 border-t border-white/10 pt-3">
			<span className="text-xs uppercase tracking-wide text-white/40">{t("sources")}</span>
			<div className="flex flex-wrap gap-2">
				{citations.map((citation, index) => {
					const isActive = index === activeIndex;
					// Citations arrive ranked best-first, so index 0 is the strongest match.
					const isTop = index === 0;
					return (
						<button
							type="button"
							key={`${citation.manualId}-${citation.page}`}
							onClick={() => onSelect(index)}
							className={`inline-flex items-center gap-2 rounded-lg border px-2.5 py-1.5 text-xs font-semibold transition-colors ${
								isActive
									? "border-primary bg-primary/10 text-white"
									: "border-white/10 text-white/70 hover:border-primary hover:text-white"
							}`}
						>
							<span>{citation.page != null ? t("page", { page: citation.page }) : t("unknown-page")}</span>
							{isTop && (
								<span className="rounded bg-primary/20 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-primary">
									{t("top-match")}
								</span>
							)}
						</button>
					);
				})}
			</div>
		</div>
	);
};
