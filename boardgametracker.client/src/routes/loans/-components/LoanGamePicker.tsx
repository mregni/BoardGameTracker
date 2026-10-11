import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { FormFieldWrapper } from "@/components/BgtForm/FormFieldWrapper";
import { SearchInputField } from "@/components/BgtForm/SearchInputField";

export interface LoanGameOption {
	id: number;
	title: string;
	image: string | null;
	borrower: string | null;
	available: boolean;
}

interface Props {
	label: string;
	options: LoanGameOption[];
	value: number | null;
	onChange: (gameId: number) => void;
	errors?: string[];
	disabled?: boolean;
}

export const LoanGamePicker = ({ label, options, value, onChange, errors = [], disabled = false }: Props) => {
	const { t } = useTranslation("loans");
	const [search, setSearch] = useState("");

	const filtered = useMemo(() => {
		const term = search.trim().toLowerCase();
		return options.filter((option) => option.id === value || option.title.toLowerCase().includes(term));
	}, [options, search, value]);

	return (
		<FormFieldWrapper label={label} errors={errors}>
			<div className="flex flex-col gap-2">
				<SearchInputField
					value={search}
					onChange={(event) => setSearch(event.target.value)}
					placeholder={t("new.game.search")}
				/>
				<div
					className={cx(
						"flex max-h-56 flex-col gap-0.5 overflow-y-auto rounded-lg border p-1",
						errors.length > 0 ? "border-error" : "border-primary/20",
					)}
				>
					{filtered.length === 0 && <span className="px-3 py-2 text-sm text-white/50">{t("new.game.empty")}</span>}
					{filtered.map((option) => {
						const selected = option.id === value;
						const unavailable = !option.available;

						return (
							<button
								key={option.id}
								type="button"
								aria-pressed={selected}
								disabled={disabled || unavailable}
								onClick={() => onChange(option.id)}
								className={cx(
									"flex items-center justify-between gap-3 rounded-md px-2 py-1.5 text-left text-sm",
									selected ? "bg-primary/40 text-white" : "text-white/90 hover:bg-primary/20",
									unavailable && "cursor-not-allowed text-white/40 hover:bg-transparent",
								)}
							>
								<span className="flex min-w-0 items-center gap-2">
									<BgtAvatar size="small" title={option.title} image={option.image} disabled={unavailable} />
									<span className="truncate">{option.title}</span>
								</span>
								{unavailable && (
									<span className="shrink-0 rounded-full bg-white/5 px-2 py-0.5 text-xs text-white/60">
										{option.borrower ? t("new.game.on-loan-to", { name: option.borrower }) : t("new.game.on-loan")}
									</span>
								)}
							</button>
						);
					})}
				</div>
			</div>
		</FormFieldWrapper>
	);
};
